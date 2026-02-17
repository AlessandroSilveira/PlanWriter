using System;

namespace PlanWriter.Domain.Dtos.Projects;

public sealed record ProjectEventWithEventRow(
    Guid Id,
    Guid ProjectId,
    Guid EventId,
    int? TargetWords,
    bool Won,
    DateTime? ValidatedAtUtc,
    int? FinalWordCount,
    int? ValidatedWords,
    string? ValidationSource,

    // Event
    string EventName,
    string EventSlug,
    int EventType,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    int? DefaultTargetWords,
    bool IsActive
);
