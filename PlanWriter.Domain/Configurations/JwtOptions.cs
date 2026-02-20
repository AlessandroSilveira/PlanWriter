using System.Collections.Generic;

namespace PlanWriter.Domain.Configurations;

public sealed class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string CurrentKid { get; set; } = "v1";
    public int ClockSkewSeconds { get; set; } = 30;
    public IReadOnlyList<JwtPreviousKeyOptions> PreviousKeys { get; set; } = [];
}

public sealed class JwtPreviousKeyOptions
{
    public string Kid { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}
