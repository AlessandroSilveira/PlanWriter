using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Auth.Commands;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using Xunit;

namespace PlanWriter.Tests.Auth.Commands;

public class LogoutAllSessionsCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ILogger<LogoutAllSessionsCommandHandler>> _logger = new();
    private readonly TimeProvider _timeProvider = new FixedTimeProvider(
        new DateTimeOffset(2026, 2, 20, 22, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Handle_ShouldReturnRevokedCount()
    {
        var userId = Guid.NewGuid();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _refreshTokenRepository
            .Setup(r => r.RevokeAllByUserAsync(userId, now, "LogoutAllSessions", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var handler = new LogoutAllSessionsCommandHandler(
            _refreshTokenRepository.Object,
            _timeProvider,
            _logger.Object);

        var result = await handler.Handle(
            new LogoutAllSessionsCommand(userId, "127.0.0.1", "tests"),
            CancellationToken.None);

        result.Should().Be(3);
        _refreshTokenRepository.Verify(
            r => r.RevokeAllByUserAsync(userId, now, "LogoutAllSessions", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
