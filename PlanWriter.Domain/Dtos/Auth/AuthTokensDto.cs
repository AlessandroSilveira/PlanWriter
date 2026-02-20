using System;

namespace PlanWriter.Domain.Dtos.Auth;

public sealed class AuthTokensDto
{
    public string AccessToken { get; init; } = string.Empty;
    public int AccessTokenExpiresInSeconds { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}
