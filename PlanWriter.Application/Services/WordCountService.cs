// PlanWriter.Application/Services/WordCountService.cs

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.Application.Services;

public class WordCountService : IWordCountService
{
    // palavra = grupos unicode de letras/números, aceita apóstrofo/hífen interno
    static readonly Regex WordRx = new(@"\b[\p{L}\p{N}]+(?:['’\-][\p{L}\p{N}]+)?\b",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    public ValidationResultDto FromText(string text, int? goal, Guid? projectId)
    {
        var normalized = NormalizeMarkdownish(text ?? "");
        return CountCore(normalized, goal, projectId);
    }

    public ValidationResultDto FromPlainFile(Stream fileStream, int? goal, Guid? projectId)
    {
        using var sr = new StreamReader(fileStream, detectEncodingFromByteOrderMarks: true);
        var content = sr.ReadToEnd();
        return FromText(content, goal, projectId);
    }

    public ValidationResultDto FromDocx(Stream fileStream, int? goal, Guid? projectId)
    {
        // DOCX: extração simples só de parágrafos (sem imagens/footnotes)
        // Requer pacote: DocumentFormat.OpenXml (OpenXML SDK)
        using var mem = new MemoryStream();
        fileStream.CopyTo(mem);
        mem.Position = 0;

        string text = ExtractDocxText(mem);
        return FromText(text, goal, projectId);
    }

    static string NormalizeMarkdownish(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        // Remove blocos de código cercados por ``` ```
        s = Regex.Replace(s, "```[\\s\\S]*?```", " ", RegexOptions.Multiline);
        // Remove cabeçalhos/links MD mais óbvios
        s = Regex.Replace(s, @"^#{1,6}\s+", "", RegexOptions.Multiline);
        s = Regex.Replace(s, @"!\[[^\]]*\]\([^)]+\)", " ", RegexOptions.Multiline); // imagens
        s = Regex.Replace(s, @"\[[^\]]*\]\([^)]+\)", " ", RegexOptions.Multiline); // links
        // Normaliza quebras
        s = s.Replace("\r\n", "\n");
        return s;
    }

    static ValidationResultDto CountCore(string content, int? goal, Guid? projectId)
    {
        content ??= string.Empty;
        var words = WordRx.Matches(content).Count;
        var chars = content.Length;
        var charsNoSpaces = content.Count(c => !char.IsWhiteSpace(c));

        // Parágrafo simples: blocos separados por linha em branco
        var paras = Regex.Split(content.Trim(), @"(\r?\n\s*\r?\n)+").Where(p => !string.IsNullOrWhiteSpace(p)).Count();

        return new ValidationResultDto(
            Words: words,
            Characters: chars,
            CharactersNoSpaces: charsNoSpaces,
            Paragraphs: paras,
            MeetsGoal: goal.HasValue && words >= goal.Value,
            Goal: goal,
            ProjectId: projectId,
            ValidatedAtUtc: DateTime.UtcNow
        );
    }

    static string ExtractDocxText(Stream mem)
    {
        // Usando OpenXML sem dependências externas
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(mem, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            sb.AppendLine(para.InnerText);
        }
        return sb.ToString();
    }
}