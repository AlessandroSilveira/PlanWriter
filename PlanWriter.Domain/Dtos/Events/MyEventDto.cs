using System;
using System.Text.Json.Serialization;

namespace PlanWriter.Domain.Dtos.Events;

public class MyEventDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = null!;

    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool? EventIsActive { get; set; }

    public Guid? ProjectId { get; set; }
    public string? ProjectTitle { get; set; }

    public int? TargetWords { get; set; }
    [JsonIgnore]
    public int? EventDefaultTargetWords { get; set; }
    public int? TotalWrittenInEvent { get; set; }

    [JsonIgnore]
    public int? FinalWordCountSnapshot { get; set; }
    [JsonIgnore]
    public int? ValidatedWordsSnapshot { get; set; }
    [JsonIgnore]
    public DateTime? ValidatedAtUtc { get; set; }
    [JsonIgnore]
    public bool? PersistedWon { get; set; }

    public int Percent { get; set; }
    public bool Won { get; set; }

    // Winner flow foundation: lets UI/backend distinguish active vs closed participations
    // without relying on raw IsActive from legacy rows.
    public string? EffectiveStatus { get; set; }
    public bool IsEffectivelyActive { get; set; }
}
