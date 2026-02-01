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

public class FollowByUsernameCommandHandlerTests
{
    private readonly Mock<IBuddiesRepository> _buddiesRepositoryMock = new();
    private readonly Mock<IUserFollowRepository> _userFollowRepositoryMock = new();
    private readonly Mock<ILogger<FollowByUsernameCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldFollowUser_WhenUsernameExistsAndNotAlreadyFollowing()
    {
        // Arrange
        var me = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        var username = "alice";
        var ct = CancellationToken.None;

        _buddiesRepositoryMock
            .Setup(r => r.FindUserIdByUsernameAsync(username, ct))
            .ReturnsAsync(followeeId);

        _userFollowRepositoryMock
            .Setup(r => r.ExistsAsync(me, followeeId, ct))
            .ReturnsAsync(false);

        _userFollowRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<UserFollow>(), ct))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new FollowByUsernameCommand(me, username);

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
        var username = "bob";
        var ct = CancellationToken.None;

        _buddiesRepositoryMock
            .Setup(r => r.FindUserIdByUsernameAsync(username, ct))
            .ReturnsAsync(followeeId);

        _userFollowRepositoryMock
            .Setup(r => r.ExistsAsync(me, followeeId, ct))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var command = new FollowByUsernameCommand(me, username);

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
        var username = "me";
        var ct = CancellationToken.None;

        _buddiesRepositoryMock
            .Setup(r => r.FindUserIdByUsernameAsync(username, ct))
            .ReturnsAsync(me);

        var handler = CreateHandler();
        var command = new FollowByUsernameCommand(me, username);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, ct);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Você não pode seguir a si mesmo.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUsernameNotFound()
    {
        // Arrange
        var me = Guid.NewGuid();
        var username = "ghost";
        var ct = CancellationToken.None;

        _buddiesRepositoryMock
            .Setup(r => r.FindUserIdByUsernameAsync(username, ct))
            .ReturnsAsync((Guid?)null);

        var handler = CreateHandler();
        var command = new FollowByUsernameCommand(me, username);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, ct);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Usuário não encontrado.");
    }

    /* ===================== HELPERS ===================== */

    private FollowByUsernameCommandHandler CreateHandler()
    {
        return new FollowByUsernameCommandHandler(
            _buddiesRepositoryMock.Object,
            _userFollowRepositoryMock.Object,
            _loggerMock.Object
        );
    }
}