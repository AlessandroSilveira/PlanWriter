using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Buddies.Dtos.Queries;
using PlanWriter.Application.Buddies.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Buddies.Commands;

public class BuddiesLeaderboardQueryHandlerTests
{
    private readonly Mock<IUserFollowRepository> _userFollowRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUserReadRepository> _userReadRepositoryMock = new();
    private readonly Mock<IProjectProgressRepository> _projectProgressRepositoryMock = new();
    private readonly Mock<ILogger<BuddiesLeaderboardQueryHandler>> _loggerMock = new();
    private readonly Mock<IProjectProgressReadRepository> _progressReadRepo = new();

    [Fact]
    public async Task Handle_ShouldReturnLeaderboard_WithTotalsAndPaceDelta()
    {
        // Arrange
        var me = Guid.NewGuid();
        var buddy = Guid.NewGuid();
        var ct = CancellationToken.None;

        var query = new BuddiesLeaderboardQuery(me, null, null);

        _userFollowRepositoryMock
            .Setup(r => r.GetFolloweeIdsAsync(me, ct))
            .ReturnsAsync(new List<Guid> { buddy });

        // 👇 Dictionary<Guid,int> explícito
        var totals = new Dictionary<Guid, int>
        {
            { me, 1000 },
            { buddy, 1500 }
        };

        _progressReadRepo
            .Setup(r => r.GetTotalWordsByUsersAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(totals);

        _userReadRepositoryMock
            .Setup(r => r.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                new User { Id = me, FirstName = "Me", LastName = "User", DisplayName = "Me" },
                new User { Id = buddy, FirstName = "Buddy", LastName = "User", DisplayName = "Buddy" }
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().HaveCount(2);

        var buddyRow = result.First();
        buddyRow.UserId.Should().Be(buddy);
        buddyRow.Total.Should().Be(1500);
        buddyRow.PaceDelta.Should().Be(500);
        buddyRow.IsMe.Should().BeFalse();

        var meRow = result.Last();
        meRow.UserId.Should().Be(me);
        meRow.Total.Should().Be(1000);
        meRow.PaceDelta.Should().Be(0);
        meRow.IsMe.Should().BeTrue();
    }
    
    [Fact]
    public async Task Handle_ShouldAssignZero_WhenUserHasNoTotal()
    {
        // Arrange
        var me = Guid.NewGuid();
        var buddy = Guid.NewGuid();
        var ct = CancellationToken.None;

        me.Should().NotBe(buddy); // só pra garantir

        var query = new BuddiesLeaderboardQuery(me, null, null);

        _userFollowRepositoryMock
            .Setup(r => r.GetFolloweeIdsAsync(me, ct))
            .ReturnsAsync(new List<Guid> { buddy });

        // 👇 apenas "me" tem total, buddy NÃO
        var totals = new Dictionary<Guid, int>
        {
            { me, 800 }
        };

        _progressReadRepo
            .Setup(r => r.GetTotalWordsByUsersAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(totals);

        _userReadRepositoryMock
            .Setup(r => r.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                new User { Id = me, FirstName = "Me", LastName = "User", DisplayName = "Me" },
                new User { Id = buddy, FirstName = "Buddy", LastName = "User", DisplayName = "Buddy" }
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().HaveCount(2);

        // ✅ nunca use First/Last aqui (lista é ordenada)
        var buddyRow = result.Single(r => r.UserId == buddy);
        buddyRow.Total.Should().Be(0);
        buddyRow.PaceDelta.Should().Be(-800);
        buddyRow.IsMe.Should().BeFalse();

        var meRow = result.Single(r => r.UserId == me);
        meRow.Total.Should().Be(800);
        meRow.PaceDelta.Should().Be(0);
        meRow.IsMe.Should().BeTrue();
    }





    [Fact]
    public async Task Handle_ShouldReturnOnlyMe_WhenNoBuddies()
    {
        // Arrange
        var me = Guid.NewGuid();
        var ct = CancellationToken.None;

        var query = new BuddiesLeaderboardQuery(me, null, null);

        _userFollowRepositoryMock
            .Setup(r => r.GetFolloweeIdsAsync(me, ct))
            .ReturnsAsync(new List<Guid>());

        var totals = new Dictionary<Guid, int>
        {
            { me, 500 }
        };

        _progressReadRepo
            .Setup(r => r.GetTotalWordsByUsersAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(totals);

        _userReadRepositoryMock
            .Setup(r => r.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                new User { Id = me, FirstName = "Me", LastName = "User", DisplayName = "Me" }
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().HaveCount(1);
        result.Single().UserId.Should().Be(me);
        result.Single().Total.Should().Be(500);
        result.Single().IsMe.Should().BeTrue();
    }

    /* ===================== HELPERS ===================== */

    private BuddiesLeaderboardQueryHandler CreateHandler()
    {
        return new BuddiesLeaderboardQueryHandler(
            _userFollowRepositoryMock.Object,
            _userReadRepositoryMock.Object,
            _loggerMock.Object,
            _progressReadRepo.Object
        );
    }
}