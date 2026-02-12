using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Projects;

public class ProjectProgressReadRepositoryTests
{
    [Fact]
    public async Task GetLastTotalBeforeAsync_ShouldReturnZero_WhenNoRow()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = new ProjectProgressReadRepository(db.Object);
        var total = await sut.GetLastTotalBeforeAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        total.Should().Be(0);
    }

    [Fact]
    public async Task GetAccumulatedAsync_ShouldUseMinutesColumn_WhenUnitIsMinutes()
    {
        var db = new Mock<IDbExecutor>();
        string? capturedSql = null;
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((sql, _, _) => capturedSql = sql)
            .ReturnsAsync(30);

        var sut = new ProjectProgressReadRepository(db.Object);
        var value = await sut.GetAccumulatedAsync(Guid.NewGuid(), GoalUnit.Minutes, CancellationToken.None);

        value.Should().Be(30);
        capturedSql.Should().Contain("SUM(Minutes)");
    }

    [Fact]
    public async Task GetTotalWordsByProjectIdsAsync_ShouldReturnEmpty_WhenIdsAreEmpty()
    {
        var db = new Mock<IDbExecutor>();
        var sut = new ProjectProgressReadRepository(db.Object);

        var result = await sut.GetTotalWordsByProjectIdsAsync(Array.Empty<Guid>());

        result.Should().BeEmpty();
        db.Verify(x => x.QueryAsync<(Guid ProjectId, int Total)>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTotalWordsByUsersAsync_ShouldMapRowsToDictionary()
    {
        var userId = Guid.NewGuid();
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<(Guid UserId, int Total)>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { (userId, 1500) });

        var sut = new ProjectProgressReadRepository(db.Object);

        var result = await sut.GetTotalWordsByUsersAsync(new[] { userId }, null, null);

        result[userId].Should().Be(1500);
    }

    [Fact]
    public async Task GetEventProjectProgressAsync_ShouldReturnNull_WhenProjectNotInEvent()
    {
        var db = new Mock<IDbExecutor>();
        var sut = new ProjectProgressReadRepository(db.Object);

        var result = await sut.GetEventProjectProgressAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProgressByProjectIdAsync_ShouldReturnRows()
    {
        var rows = new[] { new ProjectProgress { Id = Guid.NewGuid() } };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProjectProgress>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetProgressByProjectIdAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(1);
    }
}
