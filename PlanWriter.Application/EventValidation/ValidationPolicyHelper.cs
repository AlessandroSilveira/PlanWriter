using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanWriter.Application.EventValidation;

public static class ValidationPolicyHelper
{
    public const string SourceCurrent = "current";
    public const string SourcePaste = "paste";
    public const string SourceManual = "manual";

    private static readonly string[] DefaultOrderedSources =
    [
        SourceCurrent,
        SourcePaste,
        SourceManual
    ];

    public static string NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return SourceManual;

        return source.Trim().ToLowerInvariant();
    }

    public static IReadOnlyList<string> ParseAllowedSources(string? allowedValidationSources)
    {
        if (string.IsNullOrWhiteSpace(allowedValidationSources))
            return DefaultOrderedSources;

        var known = new HashSet<string>(DefaultOrderedSources, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var parsed = new List<string>();

        var tokens = allowedValidationSources
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            var normalized = NormalizeSource(token);
            if (!known.Contains(normalized) || !seen.Add(normalized))
                continue;

            parsed.Add(normalized);
        }

        return parsed.Count == 0
            ? DefaultOrderedSources
            : parsed;
    }

    public static string NormalizeAllowedSources(string? allowedValidationSources)
        => string.Join(",", ParseAllowedSources(allowedValidationSources));

    public static (DateTime StartsAtUtc, DateTime EndsAtUtc) ResolveValidationWindow(
        DateTime eventStartsAtUtc,
        DateTime eventEndsAtUtc,
        DateTime? validationWindowStartsAtUtc,
        DateTime? validationWindowEndsAtUtc)
    {
        var startsAtUtc = validationWindowStartsAtUtc ?? eventStartsAtUtc;
        var endsAtUtc = validationWindowEndsAtUtc ?? eventEndsAtUtc;

        if (endsAtUtc < startsAtUtc)
            throw new InvalidOperationException("Configuração de janela de validação inválida para o evento.");

        return (startsAtUtc, endsAtUtc);
    }
}
