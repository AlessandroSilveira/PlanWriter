using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlanWriter.Application.Auth.Commands;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using Xunit;

namespace PlanWriter.Tests.Auth.Commands;

public class RefreshSessionCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUserReadRepository> _userReadRepository = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<ILogger<RefreshSessionCommandHandler>> _logger = new();
    private readonly IOptions<AuthTokenOptions> _options = Options.Create(new AuthTokenOptions
    {
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    });
    private readonly TimeProvider _timeProvider = new FixedTimeProvider(
        new DateTimeOffset(2026, 2, 20, 21, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTokenNotFound()
    {
        _refreshTokenRepository
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenSession?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RefreshSessionCommand(new RefreshTokenDto
        {
            RefreshToken = "missing-token"
        }), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldRotateToken_WhenValid()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var userId = Guid.NewGuid();
        var oldTokenId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "writer@planwriter.com" };

        _refreshTokenRepository
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenSession
            {
                Id = oldTokenId,
                UserId = userId,
                FamilyId = familyId,
                ExpiresAtUtc = now.AddDays(1)
            });

        _userReadRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _refreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshTokenSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _refreshTokenRepository
            .Setup(r => r.MarkRotatedAsync(oldTokenId, It.IsAny<Guid>(), now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _refreshTokenRepository
            .Setup(r => r.UpdateLastUsedAsync(oldTokenId, now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _jwtTokenGenerator
            .Setup(g => g.Generate(user))
            .Returns("access-token");

        var handler = CreateHandler();
        var result = await handler.Handle(new RefreshSessionCommand(
            new RefreshTokenDto { RefreshToken = "old-refresh-token" },
            "127.0.0.1",
            "device"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.AccessTokenExpiresInSeconds.Should().Be(900);
        result.RefreshTokenExpiresAtUtc.Should().Be(now.AddDays(7));

        _refreshTokenRepository.Verify(
            r => r.CreateAsync(It.IsAny<RefreshTokenSession>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _refreshTokenRepository.Verify(
            r => r.MarkRotatedAsync(oldTokenId, It.IsAny<Guid>(), now, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRevokeFamily_AndThrow_WhenReusedTokenIsDetected()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var familyId = Guid.NewGuid();

        _refreshTokenRepository
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FamilyId = familyId,
                RevokedAtUtc = now.AddMinutes(-5),
                ExpiresAtUtc = now.AddDays(1)
            });

        _refreshTokenRepository
            .Setup(r => r.RevokeFamilyAsync(familyId, now, "RefreshTokenReuseDetected", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var handler = CreateHandler();

        Func<Task> act = async () => await handler.Handle(
            new RefreshSessionCommand(new RefreshTokenDto { RefreshToken = "reused-token" }),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Sessão inválida.");
    }

    private RefreshSessionCommandHandler CreateHandler()
    {
        return new RefreshSessionCommandHandler(
            _refreshTokenRepository.Object,
            _userReadRepository.Object,
            _jwtTokenGenerator.Object,
            _timeProvider,
            _options,
            _logger.Object);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
