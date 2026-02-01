
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Buddies.Commands;
using PlanWriter.Application.Buddies.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Buddies.Commands;

public class UnfollowCommandHandlerTests
{
    private readonly Mock<IUserFollowRepository> _userFollowRepositoryMock = new();
    private readonly Mock<ILogger<UnfollowCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldRemoveFollow_WhenCalled()
    {
        // Arrange
        var me = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _userFollowRepositoryMock
            .Setup(r => r.RemoveAsync(me, followeeId, ct))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new UnfollowCommand(me, followeeId);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.Should().Be(Unit.Value);

        _userFollowRepositoryMock.Verify(
            r => r.RemoveAsync(me, followeeId, ct),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var me = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _userFollowRepositoryMock
            .Setup(r => r.RemoveAsync(me, followeeId, ct))
            .ThrowsAsync(new Exception("DB error"));

        var handler = CreateHandler();
        var command = new UnfollowCommand(me, followeeId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, ct);

        // Assert
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("DB error");
    }
    private UnfollowCommandHandler CreateHandler()
    {
        return new UnfollowCommandHandler(_userFollowRepositoryMock.Object, _loggerMock.Object);
    }
}