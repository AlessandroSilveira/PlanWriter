namespace PlanWriter.Domain.Configurations;

public sealed class AuthTokenOptions
{
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}
