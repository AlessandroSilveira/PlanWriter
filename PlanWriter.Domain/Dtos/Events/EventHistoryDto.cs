using System;

namespace PlanWriter.Domain.Dtos.Events;

public record EventHistoryDto(
    Guid EventId,
    string EventName,
    string EventSlug,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsActive,

    Guid? ProjectId,
    string? ProjectTitle,

    int TargetWords,
    int TotalWords,
    double PercentCompleted
);