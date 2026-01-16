using System;

namespace PlanWriter.Domain.Dtos.Events;

public sealed class EventProjectProgressDto
{
    public Guid EventId { get; init; }
    public Guid ProjectId { get; init; }
    public int TotalWrittenInEvent { get; init; }
    public int TargetWords { get; init; }
    public double Percent { get; init; }
    public int? Rank { get; init; }
}
