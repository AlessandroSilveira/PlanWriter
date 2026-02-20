using FluentAssertions;
using Moq;
using PlanWriter.Application.Auth.Commands;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using Xunit;

namespace PlanWriter.Tests.Auth.Commands;

public class LogoutSessionCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly TimeProvider _timeProvider = new FixedTimeProvider(
        new DateTimeOffset(2026, 2, 20, 21, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenTokenIsNotFound()
    {
        _refreshTokenRepository
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenSession?)null);

        var handler = new LogoutSessionCommandHandler(_refreshTokenRepository.Object, _timeProvider);
        var result = await handler.Handle(new LogoutSessionCommand(new RefreshTokenDto
        {
            RefreshToken = "unknown"
        }), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRevokeFamily_WhenTokenExists()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var familyId = Guid.NewGuid();

        _refreshTokenRepository
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenSession
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                ExpiresAtUtc = now.AddDays(1)
            });

        _refreshTokenRepository
            .Setup(r => r.RevokeFamilyAsync(familyId, now, "LogoutCurrentSession", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new LogoutSessionCommandHandler(_refreshTokenRepository.Object, _timeProvider);
        var result = await handler.Handle(new LogoutSessionCommand(new RefreshTokenDto
        {
            RefreshToken = "valid"
        }), CancellationToken.None);

        result.Should().BeTrue();
        _refreshTokenRepository.Verify(
            r => r.RevokeFamilyAsync(familyId, now, "LogoutCurrentSession", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
