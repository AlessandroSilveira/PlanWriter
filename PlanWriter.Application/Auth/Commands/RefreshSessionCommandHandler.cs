using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.Security;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Application.Auth.Commands;

public sealed class RefreshSessionCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserReadRepository userReadRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    TimeProvider timeProvider,
    IOptions<AuthTokenOptions> tokenOptions,
    ILogger<RefreshSessionCommandHandler> logger)
    : IRequestHandler<RefreshSessionCommand, AuthTokensDto?>
{
    public async Task<AuthTokensDto?> Handle(RefreshSessionCommand request, CancellationToken ct)
    {
        var rawRefreshToken = request.Request.RefreshToken?.Trim();
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return null;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var refreshHash = RefreshTokenSecurity.HashToken(rawRefreshToken);
        var session = await refreshTokenRepository.GetByHashAsync(refreshHash, ct);
        if (session is null)
        {
            return null;
        }

        if (session.RevokedAtUtc.HasValue)
        {
            await refreshTokenRepository.RevokeFamilyAsync(
                session.FamilyId,
                now,
                "RefreshTokenReuseDetected",
                ct);

            logger.LogWarning(
                "Refresh token reuse detected. Family {FamilyId} revoked.",
                session.FamilyId);

            throw new UnauthorizedAccessException("Sessão inválida.");
        }

        if (session.ExpiresAtUtc <= now)
        {
            await refreshTokenRepository.RevokeAsync(
                session.Id,
                now,
                "RefreshTokenExpired",
                ct);

            return null;
        }

        var user = await userReadRepository.GetByIdAsync(session.UserId, ct);
        if (user is null)
        {
            await refreshTokenRepository.RevokeFamilyAsync(
                session.FamilyId,
                now,
                "UserNotFound",
                ct);

            return null;
        }

        var options = tokenOptions.Value;
        var newRawRefreshToken = RefreshTokenSecurity.GenerateToken();
        var newRefreshHash = RefreshTokenSecurity.HashToken(newRawRefreshToken);
        var newRefreshTokenId = Guid.NewGuid();
        var newRefreshExpiry = now.AddDays(Math.Max(1, options.RefreshTokenDays));

        await refreshTokenRepository.CreateAsync(new RefreshTokenSession
        {
            Id = newRefreshTokenId,
            UserId = session.UserId,
            FamilyId = session.FamilyId,
            ParentTokenId = session.Id,
            ReplacedByTokenId = null,
            TokenHash = newRefreshHash,
            Device = request.Device ?? session.Device,
            CreatedByIp = request.IpAddress ?? session.CreatedByIp,
            CreatedAtUtc = now,
            ExpiresAtUtc = newRefreshExpiry
        }, ct);

        var rotated = await refreshTokenRepository.MarkRotatedAsync(
            session.Id,
            newRefreshTokenId,
            now,
            ct);

        if (!rotated)
        {
            await refreshTokenRepository.RevokeAsync(
                newRefreshTokenId,
                now,
                "RotationRaceCondition",
                ct);

            return null;
        }

        await refreshTokenRepository.UpdateLastUsedAsync(
            session.Id,
            now,
            ct);

        var adminMfaVerified = !user.IsAdmin || user.AdminMfaEnabled;
        var accessToken = jwtTokenGenerator.Generate(user, adminMfaVerified);
        return new AuthTokensDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresInSeconds = Math.Max(1, options.AccessTokenMinutes) * 60,
            RefreshToken = newRawRefreshToken,
            RefreshTokenExpiresAtUtc = newRefreshExpiry
        };
    }
}
