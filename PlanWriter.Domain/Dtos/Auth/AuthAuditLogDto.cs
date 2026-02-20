using System;

namespace PlanWriter.Domain.Dtos.Auth;

public sealed class AuthAuditLogDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
    public string? CorrelationId { get; init; }
    public string? Details { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
