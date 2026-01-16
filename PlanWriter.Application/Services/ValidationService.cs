// PlanWriter.Application/Services/ValidationService.cs

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.Application.Services;

public class ValidationService(IWordCountService wordCountService, IProjectRepository projectRepository) : IValidationService
{
    public async Task<ValidationResultDto> ValidateTextAsync(Guid projectId, string text, bool save, CancellationToken ct)
    {
        var (goal, _) = await projectRepository.GetGoalAndTitleAsync(projectId, ct);
        var res = wordCountService.FromText(text ?? string.Empty, goal, projectId);

        if (save)
            await projectRepository.SaveValidationAsync(projectId, res.Words, res.MeetsGoal, res.ValidatedAtUtc, ct);

        return res;
    }

    public async Task<ValidationResultDto> ValidateUploadAsync(Guid projectId, string fileName, Stream fileStream, bool save, CancellationToken ct)
    {
        if (fileStream is null || fileStream == Stream.Null) throw new ArgumentException("Arquivo inválido.");
        var (goal, _) = await projectRepository.GetGoalAndTitleAsync(projectId, ct);

        var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
        ValidationResultDto res = ext switch
        {
            ".txt" or ".md" or ".markdown" => wordCountService.FromPlainFile(fileStream, goal, projectId),
            ".docx" => wordCountService.FromDocx(fileStream, goal, projectId),
            _ => throw new InvalidOperationException("Formato não suportado. Use .txt, .md ou .docx.")
        };

        if (save)
            await projectRepository.SaveValidationAsync(projectId, res.Words, res.MeetsGoal, res.ValidatedAtUtc, ct);

        return res;
    }

   

}