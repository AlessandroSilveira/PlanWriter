using System;
using System.Collections.Generic;
using PlanWriter.Domain.Dtos.Badges;

namespace PlanWriter.Domain.Dtos.Goodies;

public sealed class EventGoodiesDto
{
    public Guid EventId { get; set; }
    public Guid ProjectId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public int TargetWords { get; set; }
    public int TotalWords { get; set; }
    public DateTime? ValidatedAtUtc { get; set; }
    public bool Won { get; set; }
    public WinnerEligibilityDto Eligibility { get; set; } = new();
    public CertificateGoodieDto Certificate { get; set; } = new();
    public IReadOnlyList<BadgeDto> Badges { get; set; } = Array.Empty<BadgeDto>();
}

public sealed class WinnerEligibilityDto
{
    public bool IsEligible { get; set; }
    public bool CanValidate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class CertificateGoodieDto
{
    public bool Available { get; set; }
    public string? DownloadUrl { get; set; }
    public string Message { get; set; } = string.Empty;
}
