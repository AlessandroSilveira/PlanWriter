using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.Security;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Application.Auth.Commands;

public sealed class LogoutSessionCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    TimeProvider timeProvider)
    : IRequestHandler<LogoutSessionCommand, bool>
{
    public async Task<bool> Handle(LogoutSessionCommand request, CancellationToken ct)
    {
        var rawRefreshToken = request.Request.RefreshToken?.Trim();
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return true;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = RefreshTokenSecurity.HashToken(rawRefreshToken);
        var session = await refreshTokenRepository.GetByHashAsync(tokenHash, ct);
        if (session is null)
        {
            return true;
        }

        if (session.RevokedAtUtc.HasValue)
        {
            return true;
        }

        await refreshTokenRepository.RevokeFamilyAsync(
            session.FamilyId,
            now,
            "LogoutCurrentSession",
            ct);

        return true;
    }
}
