using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.Security;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Application.Auth.Commands;

public class LoginUserCommandHandler(IUserAuthReadRepository userReadRepository, IPasswordHasher<User> passwordHasher,
    IJwtTokenGenerator tokenGenerator, IRefreshTokenRepository refreshTokenRepository,
    TimeProvider timeProvider, IOptions<AuthTokenOptions> tokenOptions,
    ILogger<LoginUserCommandHandler> logger)
    : IRequestHandler<LoginUserCommand, AuthTokensDto?>
{
    public async Task<AuthTokensDto?> Handle(LoginUserCommand request, CancellationToken ct)
    {
        var email = request.Request.Email.Trim().ToLowerInvariant();

        logger.LogInformation("Login attempt for user {Email}", email);

        var user = await userReadRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            logger.LogWarning("Login failed for {Email}: user not found", email);
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Request.Password);

        if (result != PasswordVerificationResult.Success)
        {
            logger.LogWarning("Login failed for {Email}: invalid password", email);
            return null;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var options = tokenOptions.Value;
        var accessToken = tokenGenerator.Generate(user);

        var refreshToken = RefreshTokenSecurity.GenerateToken();
        var refreshTokenHash = RefreshTokenSecurity.HashToken(refreshToken);
        var refreshExpiry = now.AddDays(Math.Max(1, options.RefreshTokenDays));
        var refreshSessionId = Guid.NewGuid();

        await refreshTokenRepository.CreateAsync(new RefreshTokenSession
        {
            Id = refreshSessionId,
            UserId = user.Id,
            FamilyId = refreshSessionId,
            ParentTokenId = null,
            ReplacedByTokenId = null,
            TokenHash = refreshTokenHash,
            Device = request.Device,
            CreatedByIp = request.IpAddress,
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpiry
        }, ct);

        logger.LogInformation("User {Email} logged in successfully", email);

        return new AuthTokensDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresInSeconds = Math.Max(1, options.AccessTokenMinutes) * 60,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiry
        };
    }
}
