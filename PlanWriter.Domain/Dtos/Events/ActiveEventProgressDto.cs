using System;

namespace PlanWriter.Domain.Dtos.Events;

public class ActiveEventProgressDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; }

    public int TargetWords { get; set; }
    public int TotalWritten { get; set; }
    public double Percent { get; set; }

    public bool IsCompleted { get; set; }
    public int? Rank { get; set; }
}
