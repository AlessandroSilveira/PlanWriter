using System;
using System.Collections.Generic;

namespace PlanWriter.Application.EventValidation.Dtos.Queries;

public sealed record ValidationStatusDto(
    int TargetWords,
    int TotalWords,
    bool IsValidated,
    DateTime? ValidatedAtUtc,
    int? ValidatedWords,
    string? ValidationSource,
    DateTime ValidationWindowStartsAtUtc,
    DateTime ValidationWindowEndsAtUtc,
    bool IsWithinValidationWindow,
    bool CanValidate,
    string? BlockReason,
    IReadOnlyList<string> AllowedSources
);
