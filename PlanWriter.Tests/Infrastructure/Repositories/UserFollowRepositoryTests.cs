using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories;

public class UserFollowRepositoryTests
{
    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenCountIsPositive()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new UserFollowRepository(db.Object);
        var result = await sut.ExistsAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_ShouldExecuteInsert()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new UserFollowRepository(db.Object);

        await sut.AddAsync(new UserFollow
        {
            FollowerId = Guid.NewGuid(),
            FolloweeId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow
        }, CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFolloweeIdsAsync_ShouldReturnRowsAsList()
    {
        var followeeIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<Guid>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(followeeIds);

        var sut = new UserFollowRepository(db.Object);
        var result = await sut.GetFolloweeIdsAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEquivalentTo(followeeIds);
    }
}
