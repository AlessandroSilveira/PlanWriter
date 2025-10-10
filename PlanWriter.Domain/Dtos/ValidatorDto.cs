// PlanWriter.Application/DTOs/ValidationDtos.cs

using System;

namespace PlanWriter.Domain.Dtos;

public record ValidateTextRequest(Guid ProjectId, string Text, bool Save = true);
public record ValidateUploadRequest(Guid ProjectId, bool Save = true);

public record ValidationResultDto(
    int Words,
    int Characters,
    int CharactersNoSpaces,
    int Paragraphs,
    bool MeetsGoal,
    int? Goal,
    Guid? ProjectId,
    DateTime ValidatedAtUtc
);