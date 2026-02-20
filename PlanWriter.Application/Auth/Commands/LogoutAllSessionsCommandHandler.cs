using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Application.Auth.Commands;

public sealed class LogoutAllSessionsCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    TimeProvider timeProvider,
    ILogger<LogoutAllSessionsCommandHandler> logger)
    : IRequestHandler<LogoutAllSessionsCommand, int>
{
    public async Task<int> Handle(LogoutAllSessionsCommand request, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var revokedSessions = await refreshTokenRepository.RevokeAllByUserAsync(
            request.UserId,
            now,
            "LogoutAllSessions",
            ct);

        logger.LogInformation(
            "Global logout completed for user {UserId}. RevokedSessions={RevokedSessions}. IpAddress={IpAddress}. Device={Device}",
            request.UserId,
            revokedSessions,
            request.IpAddress ?? "unknown",
            request.Device ?? "unknown");

        return revokedSessions;
    }
}
