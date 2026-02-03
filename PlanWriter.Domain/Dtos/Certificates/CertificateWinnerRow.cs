using System;

namespace PlanWriter.Domain.Dtos.Certificates;

public sealed class CertificateWinnerRow
{
    public string EventName { get; init; } = default!;
    public DateTime? ValidatedAtUtc { get; init; }
    public bool Won { get; init; }
    public int FinalWordCount { get; init; }
    public string ProjectTitle { get; init; } = "Projeto";
}