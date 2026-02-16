using System;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Application.WordWar.Dtos.Queries;

public class WordWarDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public WordWarStatus Status { get; set; } 
    public int DurationMinutes { get; set; } 
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime FinishedAtUtc { get; set; }
    
}