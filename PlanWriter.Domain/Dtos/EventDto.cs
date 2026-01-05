using System;

namespace PlanWriter.Domain.Dtos
{
    public record EventDto(Guid Id, string Name, string Slug, string Type,
        DateTime StartsAtUtc, DateTime EndsAtUtc,
        int? DefaultTargetWords, bool IsActive);

    public record CreateEventRequest(string Name, string Type,
        DateTime StartsAtUtc, DateTime EndsAtUtc,
        int? DefaultTargetWords);

    public record JoinEventRequest(Guid ProjectId, Guid EventId, int? TargetWords);

    public record FinalizeRequest(Guid ProjectEventId);

    public record EventProgressDto(
        Guid ProjectId,
        Guid EventId,
        int TargetWords,
        int TotalWrittenInEvent,
        int Percent,
        int Remaining,
        int Days,
        int DayIndex,       // 1..Days
        int DailyTarget,     // ceil(TargetWords / Days)
        Guid ProjectEventId,         // NEW
        DateTime? ValidatedAtUtc,    // NEW
        bool Won
    );
}