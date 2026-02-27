using System;
using System.Collections.Generic;

namespace PlanWriter.Domain.Dtos.Events;

public sealed class EventParticipantStatusDto
{
    public Guid EventId { get; set; }
    public Guid ProjectId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;

    public string EventStatus { get; set; } = string.Empty;
    public bool IsEventActive { get; set; }
    public bool IsEventClosed { get; set; }
    public DateTime EventStartsAtUtc { get; set; }
    public DateTime EventEndsAtUtc { get; set; }

    public DateTime ValidationWindowStartsAtUtc { get; set; }
    public DateTime ValidationWindowEndsAtUtc { get; set; }
    public bool IsValidationWindowOpen { get; set; }

    public int TargetWords { get; set; }
    public int TotalWords { get; set; }
    public int Percent { get; set; }
    public int RemainingWords { get; set; }

    public bool IsValidated { get; set; }
    public bool IsWinner { get; set; }
    public bool IsEligible { get; set; }
    public bool CanValidate { get; set; }

    public string EligibilityStatus { get; set; } = string.Empty;
    public string EligibilityMessage { get; set; } = string.Empty;
    public string? ValidationBlockReason { get; set; }

    public DateTime? ValidatedAtUtc { get; set; }
    public int? ValidatedWords { get; set; }
    public string? ValidationSource { get; set; }

    public IReadOnlyList<string> AllowedValidationSources { get; set; } = Array.Empty<string>();
}
