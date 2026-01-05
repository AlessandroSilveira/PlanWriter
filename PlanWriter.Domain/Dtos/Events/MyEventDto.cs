using System;

namespace PlanWriter.Domain.Dtos.Events;

public class MyEventDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = null!;

    public Guid? ProjectId { get; set; }
    public string? ProjectTitle { get; set; }

    public int? TargetWords { get; set; }
    public int? TotalWrittenInEvent { get; set; }

    public int Percent { get; set; }
}
