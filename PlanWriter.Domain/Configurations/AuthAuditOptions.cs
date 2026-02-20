namespace PlanWriter.Domain.Configurations;

public sealed class AuthAuditOptions
{
    public int RetentionDays { get; set; } = 180;
    public int MaxReadLimit { get; set; } = 500;
}
