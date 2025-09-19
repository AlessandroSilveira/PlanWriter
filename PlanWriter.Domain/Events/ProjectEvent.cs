using System;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Events;

public class ProjectEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid EventId { get; set; }
    public int? TargetWords { get; set; }
    public int? FinalWordCount { get; set; }
    public DateTime? ValidatedAtUtc { get; set; }
    public bool Won { get; set; } 
    public Event? Event { get; set; }
    public Project? Project { get; set; } 
    public int? ValidatedWords { get; set; }           // total certificado
    public string? ValidationSource { get; set; }
}