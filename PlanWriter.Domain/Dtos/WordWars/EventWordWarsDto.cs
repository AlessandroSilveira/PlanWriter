using System;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Dtos.WordWars;

public class EventWordWarsDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public WordWarStatus Status { get; set; }
    public int DurationInMinuts { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}
