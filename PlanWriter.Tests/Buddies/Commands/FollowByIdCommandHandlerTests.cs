using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Buddies.Commands;
using PlanWriter.Application.Buddies.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Buddies.Commands;

public class FollowByIdCommandHandlerTests
{
    private readonly Mock<IUserFollowRepository> _userFollowRepositoryMock = new();
    private readonly Mock<ILogger<FollowByIdCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldFollowUser_WhenNotAlreadyFollowing()
    {
        // Arrange
        var me = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _userFollowRepositoryMock
            .Setup(r => r.ExistsAsync(me, followeeId, ct))
            .ReturnsAsync(false);

        _userFollowRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<UserFollow>(), ct))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new FollowByIdCommand(me, followeeId);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.Should().Be(Unit.Value);

        _userFollowRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<UserFollow>(uf =>
                    uf.FollowerId == me &&
                    uf.FolloweeId == followeeId),
                ct),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldNotAddFollow_WhenAlreadyFollowing()
    {
        // Arrange
        var me = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _userFollowRepositoryMock
            .Setup(r => r.ExistsAsync(me, followeeId, ct))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var command = new FollowByIdCommand(me, followeeId);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.Should().Be(Unit.Value);

        _userFollowRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<UserFollow>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserTriesToFollowHimself()
    {
        // Arrange
        var me = Guid.NewGuid();
        var ct = CancellationToken.None;

        var handler = CreateHandler();
        var command = new FollowByIdCommand(me, me);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, ct);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Você não pode seguir a si mesmo.");
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var me = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _userFollowRepositoryMock
            .Setup(r => r.ExistsAsync(me, followeeId, ct))
            .ThrowsAsync(new Exception("DB error"));

        var handler = CreateHandler();
        var command = new FollowByIdCommand(me, followeeId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, ct);

        // Assert
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("DB error");
    }

    /* ===================== HELPERS ===================== */

    private FollowByIdCommandHandler CreateHandler()
    {
        return new FollowByIdCommandHandler(
            _userFollowRepositoryMock.Object,
            _loggerMock.Object
        );
    }
}