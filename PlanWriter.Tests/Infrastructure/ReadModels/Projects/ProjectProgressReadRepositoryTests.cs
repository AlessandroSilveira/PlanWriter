using System.Reflection;
using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Projects;
using PlanWriter.Tests.Infrastructure;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Projects;

public class ProjectProgressReadRepositoryTests
{
    [Fact]
    public async Task GetProgressHistoryAsync_WithCancellationToken_ShouldReturnRows()
    {
        var rows = new[] { new ProgressHistoryRow(DateTime.UtcNow.Date, 100) };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProgressHistoryRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetProgressHistoryAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserProgressByDayAsync_ShouldReturnRows()
    {
        var rows = new[] { new ProgressHistoryRow(DateTime.UtcNow.Date, 250) };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProgressHistoryRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetUserProgressByDayAsync(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            null,
            CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].WordsWritten.Should().Be(250);
    }

    [Fact]
    public async Task GetUserProgressByDayAsync_ShouldPassProjectFilterParameter()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        object? capturedParam = null;
        string? capturedSql = null;

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProgressHistoryRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
            })
            .ReturnsAsync(Array.Empty<ProgressHistoryRow>());

        var sut = new ProjectProgressReadRepository(db.Object);
        await sut.GetUserProgressByDayAsync(
            userId,
            new DateTime(2026, 2, 1),
            new DateTime(2026, 2, 28),
            projectId,
            CancellationToken.None);

        capturedSql.Should().NotBeNull();
        capturedSql.Should().Contain("@ProjectId IS NULL OR pp.ProjectId = @ProjectId");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("UserId").Should().Be(userId);
        capturedParam.GetProp<Guid?>("ProjectId").Should().Be(projectId);
    }

    [Fact]
    public async Task GetMonthlyWordsAsync_WithCancellationToken_ShouldReturnValue()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(777);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetMonthlyWordsAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, CancellationToken.None);

        result.Should().Be(777);
    }

    [Fact]
    public async Task GetProgressByDayAsync_ShouldReturnRows()
    {
        var rows = new[] { new ProjectProgressDayDto(DateTime.UtcNow.Date, 100, 20, 2) };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProjectProgressDayDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetProgressByDayAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_ShouldReturnRow()
    {
        var row = new ProgressRow(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<ProgressRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(row);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetByIdAsync(row.Id, Guid.NewGuid(), CancellationToken.None);

        result.Should().Be(row);
    }

    [Fact]
    public async Task GetLastProgressBeforeAsync_WithCancellationToken_ShouldReturnRow()
    {
        var row = new ProgressRow(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<ProgressRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(row);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetLastProgressBeforeAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        result.Should().Be(row);
    }

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
    public async Task GetLastTotalBeforeAsync_ShouldReturnValue_WhenRowExists()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1234);

        var sut = new ProjectProgressReadRepository(db.Object);
        var total = await sut.GetLastTotalBeforeAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        total.Should().Be(1234);
    }

    [Fact]
    public async Task GetProgressHistoryAsync_WithoutCancellationToken_ShouldReturnRows()
    {
        var rows = new[] { new ProjectProgress { Id = Guid.NewGuid(), WordsWritten = 10 } };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProjectProgress>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetProgressHistoryAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutCancellationToken_ShouldReturnEntity()
    {
        var entity = new ProjectProgress { Id = Guid.NewGuid(), WordsWritten = 10 };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<ProjectProgress>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetByIdAsync(entity.Id, Guid.NewGuid());

        result.Should().Be(entity);
    }

    [Fact]
    public async Task GetLastProgressBeforeAsync_WithoutUser_ShouldReturnEntity()
    {
        var entity = new ProjectProgress { Id = Guid.NewGuid(), WordsWritten = 10 };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<ProjectProgress>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetLastProgressBeforeAsync(Guid.NewGuid(), DateTime.UtcNow);

        result.Should().Be(entity);
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
    public async Task GetAccumulatedAsync_ShouldUsePagesColumn_WhenUnitIsPages()
    {
        var db = new Mock<IDbExecutor>();
        string? capturedSql = null;
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((sql, _, _) => capturedSql = sql)
            .ReturnsAsync(12);

        var sut = new ProjectProgressReadRepository(db.Object);
        var value = await sut.GetAccumulatedAsync(Guid.NewGuid(), GoalUnit.Pages, CancellationToken.None);

        value.Should().Be(12);
        capturedSql.Should().Contain("SUM(Pages)");
    }

    [Fact]
    public async Task GetAccumulatedAsync_ShouldUseWordsColumn_WhenUnitIsWords()
    {
        var db = new Mock<IDbExecutor>();
        string? capturedSql = null;
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((sql, _, _) => capturedSql = sql)
            .ReturnsAsync(200);

        var sut = new ProjectProgressReadRepository(db.Object);
        var value = await sut.GetAccumulatedAsync(Guid.NewGuid(), GoalUnit.Words, CancellationToken.None);

        value.Should().Be(200);
        capturedSql.Should().Contain("SUM(WordsWritten)");
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
    public async Task GetTotalWordsByProjectIdsAsync_ShouldMapRows()
    {
        var projectId = Guid.NewGuid();
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<(Guid ProjectId, int Total)>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { (projectId, 900) });

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetTotalWordsByProjectIdsAsync(new[] { projectId });

        result[projectId].Should().Be(900);
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
    public async Task GetEventProjectProgressAsync_ShouldReturnCalculatedProgress_WhenProjectIsInEvent()
    {
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var rankRowType = typeof(ProjectProgressReadRepository).GetNestedType("RankRow", BindingFlags.NonPublic)!;
        var rankRow = Activator.CreateInstance(rankRowType, 30000, 2)!;
        var link = (TargetWords: (int?)50000, DefaultTargetWords: (int?)60000, StartsAtUtc: now.AddDays(-10), EndsAtUtc: now.AddDays(10));

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) =>
            {
                if (t == typeof((int? TargetWords, int? DefaultTargetWords, DateTime StartsAtUtc, DateTime EndsAtUtc)))
                    return link;
                if (t == rankRowType)
                    return rankRow;
                return null;
            }
        };

        var sut = new ProjectProgressReadRepository(db);
        var result = await sut.GetEventProjectProgressAsync(eventId, projectId);

        result.Should().NotBeNull();
        result!.TotalWrittenInEvent.Should().Be(30000);
        result.TargetWords.Should().Be(50000);
        result.Rank.Should().Be(2);
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
    public async Task GetTotalWordsByUsersAsync_ShouldReturnEmpty_WhenNoIds()
    {
        var db = new Mock<IDbExecutor>();
        var sut = new ProjectProgressReadRepository(db.Object);

        var result = await sut.GetTotalWordsByUsersAsync(Array.Empty<Guid>(), null, null);

        result.Should().BeEmpty();
        db.Verify(x => x.QueryAsync<(Guid UserId, int Total)>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMonthlyWordsAsync_OverloadWithoutCancellationToken_ShouldReturnValue()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(600);

        var sut = new ProjectProgressReadRepository(db.Object);
        var result = await sut.GetMonthlyWordsAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        result.Should().Be(600);
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
