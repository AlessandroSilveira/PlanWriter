using System;

namespace PlanWriter.Domain.Entities;

public class RefreshTokenSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public Guid? ParentTokenId { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string? Device { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }
}
