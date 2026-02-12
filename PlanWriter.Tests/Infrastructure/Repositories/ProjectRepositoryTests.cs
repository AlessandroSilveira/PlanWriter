using System.Collections;
using System.Reflection;
using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;
using PlanWriter.Tests.Infrastructure;
using PlanWriter.Tests.Infrastructure.Fakes;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories;

public class ProjectRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldReturnProject_WhenAffectedIsOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);
        var project = BuildProject();

        var result = await sut.CreateAsync(project, CancellationToken.None);

        result.Should().BeSameAs(project);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenAffectedIsNotOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);

        var act = () => sut.CreateAsync(BuildProject(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetGoalAsync_ShouldReturnFalse_WhenNoRowsAreAffected()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);

        var result = await sut.SetGoalAsync(Guid.NewGuid(), Guid.NewGuid(), 50000, DateTime.UtcNow.AddDays(30), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetGoalAsync_ShouldReturnTrue_WhenOneRowIsAffected()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);
        var result = await sut.SetGoalAsync(Guid.NewGuid(), Guid.NewGuid(), 50000, null, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyValidationAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);

        var act = () => sut.ApplyValidationAsync(Guid.NewGuid(), new ValidationResultDto(100, 0, 0, 0, false, null, null, DateTime.UtcNow), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ApplyValidationAsync_ShouldSucceed_WhenProjectExists()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);
        await sut.ApplyValidationAsync(Guid.NewGuid(), new ValidationResultDto(100, 0, 0, 0, true, 100, Guid.NewGuid(), DateTime.UtcNow), CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGoalAndTitleAsync_ShouldReturnTuple_WhenRowExists()
    {
        var rowType = typeof(ProjectRepository).GetNestedType("ProjectGoalTitleRow", BindingFlags.NonPublic)!;
        var row = Activator.CreateInstance(rowType, 12345, "Book")!;

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) => t == rowType ? row : null
        };

        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db);

        var result = await sut.GetGoalAndTitleAsync(Guid.NewGuid(), CancellationToken.None);

        result.goalWords.Should().Be(12345);
        result.title.Should().Be("Book");
    }

    [Fact]
    public async Task GetGoalAndTitleAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        var db = new StubDbExecutor();
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db);

        var act = () => sut.GetGoalAndTitleAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetGoalAsync_ShouldMapGoalUnit()
    {
        var rowType = typeof(ProjectRepository).GetNestedType("ProjectGoalRow", BindingFlags.NonPublic)!;
        var row = Activator.CreateInstance(rowType, 90, (int)GoalUnit.Minutes)!;

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) => t == rowType ? row : null
        };

        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db);

        var result = await sut.GetGoalAsync(Guid.NewGuid(), CancellationToken.None);

        result.goalAmount.Should().Be(90);
        result.unit.Should().Be(GoalUnit.Minutes);
    }

    [Fact]
    public async Task GetGoalAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        var db = new StubDbExecutor();
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db);

        var act = () => sut.GetGoalAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenAffectedIsNotOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);

        var act = () => sut.UpdateAsync(BuildProject(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenAffectedIsOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);
        await sut.UpdateAsync(BuildProject(), CancellationToken.None);
    }

    [Fact]
    public async Task SetProjectVisibilityAsync_ShouldThrow_WhenAffectedIsNotOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);

        var act = () => sut.SetProjectVisibilityAsync(Guid.NewGuid(), Guid.NewGuid(), true, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetProjectVisibilityAsync_ShouldSucceed_WhenAffectedIsOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);
        await sut.SetProjectVisibilityAsync(Guid.NewGuid(), Guid.NewGuid(), true, CancellationToken.None);
    }

    [Fact]
    public async Task UserOwnsProjectAsync_ShouldReturnTrue_WhenCountIsPositive()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);

        var result = await sut.UserOwnsProjectAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserOwnsProjectAsync_ShouldReturnFalse_WhenCountIsZero()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var cf = new Mock<IDbConnectionFactory>();

        var sut = new ProjectRepository(cf.Object, db.Object);
        var result = await sut.UserOwnsProjectAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldComputeAggregates_FromProgressEntries()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-5);

        var project = new Project
        {
            Id = projectId,
            UserId = userId,
            WordCountGoal = 1000,
            CreatedAt = createdAt,
            Deadline = DateTime.UtcNow.AddDays(10)
        };

        var progress = new[]
        {
            new ProjectProgress { ProjectId = projectId, Date = DateTime.UtcNow.AddDays(-2), WordsWritten = 200, TimeSpentInMinutes = 30 },
            new ProjectProgress { ProjectId = projectId, Date = DateTime.UtcNow.AddDays(-1), WordsWritten = 300, TimeSpentInMinutes = 50 }
        };

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) => t == typeof(Project) ? project : null,
            QueryAsyncHandler = (t, _, _, _) => t == typeof(ProjectProgress) ? progress : Array.Empty<ProjectProgress>()
        };

        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db);

        var stats = await sut.GetStatisticsAsync(projectId, userId);

        stats.Should().NotBeNull();
        stats.TotalWordsWritten.Should().Be(500);
        stats.RemainingWords.Should().Be(500);
        stats.TotalTimeSpentInMinutes.Should().Be(80);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnNull_WhenProjectNotFound()
    {
        var db = new StubDbExecutor();
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db);

        var result = await sut.GetStatisticsAsync(Guid.NewGuid(), Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProjectWithProgressAsync_ShouldReturnNull_WhenProjectNotFound()
    {
        var db = new StubDbExecutor();
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db);

        var result = await sut.GetProjectWithProgressAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProjectWithProgressAsync_ShouldAttachProgressEntries()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var project = new Project { Id = projectId, UserId = userId, CreatedAt = DateTime.UtcNow };
        var progress = new[] { new ProjectProgress { Id = Guid.NewGuid(), ProjectId = projectId, WordsWritten = 50 } };

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) => t == typeof(Project) ? project : null,
            QueryAsyncHandler = (t, _, _, _) => t == typeof(ProjectProgress) ? progress : Array.Empty<ProjectProgress>()
        };
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db);

        var result = await sut.GetProjectWithProgressAsync(projectId, userId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProgressEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProjectById_ShouldReturnEntity()
    {
        var expected = new Project { Id = Guid.NewGuid(), Title = "Book" };
        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) => t == typeof(Project) ? expected : null
        };
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db);

        var result = await sut.GetProjectById(expected.Id);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task SaveValidationAsync_ShouldThrow_WhenProjectNotFound()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db.Object);

        var act = () => sut.SaveValidationAsync(Guid.NewGuid(), 100, true, DateTime.UtcNow, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SaveValidationAsync_ShouldSucceed_WhenProjectExists()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db.Object);

        await sut.SaveValidationAsync(Guid.NewGuid(), 100, true, DateTime.UtcNow, CancellationToken.None);
    }

    [Fact]
    public async Task UpdateFlexibleGoalAsync_ShouldThrow_WhenProjectNotFound()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db.Object);

        var act = () => sut.UpdateFlexibleGoalAsync(Guid.NewGuid(), 120, GoalUnit.Pages, null, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateFlexibleGoalAsync_ShouldSucceed_WhenProjectExists()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db.Object);

        await sut.UpdateFlexibleGoalAsync(Guid.NewGuid(), 120, GoalUnit.Pages, null, CancellationToken.None);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnRows()
    {
        var rows = new[] { new ProjectDto { Id = Guid.NewGuid(), Title = "Book" } };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProjectDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(rows);
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db.Object);

        var result = await sut.GetByUserIdAsync(Guid.NewGuid(), CancellationToken.None);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPublicProjectsByUserIdAsync_ShouldReturnRows()
    {
        var rows = new[] { new Project { Id = Guid.NewGuid(), IsPublic = true } };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<Project>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(rows);
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db.Object);

        var result = await sut.GetPublicProjectsByUserIdAsync(Guid.NewGuid());
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnRows()
    {
        var rows = new[] { new Project { Id = Guid.NewGuid() } };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<Project>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(rows);
        var cf = new Mock<IDbConnectionFactory>();
        var sut = new ProjectRepository(cf.Object, db.Object);

        var result = await sut.GetAllAsync(CancellationToken.None);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteProjectAsync_ShouldCommitTransaction_WhenNoException()
    {
        var conn = new FakeDbConnection();
        conn.NonQueryResults.Enqueue(1);
        conn.NonQueryResults.Enqueue(1);
        conn.NonQueryResults.Enqueue(1);

        var db = new Mock<IDbExecutor>();
        var cf = new Mock<IDbConnectionFactory>();
        cf.Setup(x => x.CreateConnection()).Returns(conn);

        var sut = new ProjectRepository(cf.Object, db.Object);
        var ok = await sut.DeleteProjectAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        ok.Should().BeTrue();
        conn.LastTransaction!.Committed.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProjectAsync_ShouldRollback_WhenExceptionOccurs()
    {
        var conn = new FakeDbConnection
        {
            ExecuteNonQueryException = new InvalidOperationException("boom")
        };

        var db = new Mock<IDbExecutor>();
        var cf = new Mock<IDbConnectionFactory>();
        cf.Setup(x => x.CreateConnection()).Returns(conn);

        var sut = new ProjectRepository(cf.Object, db.Object);

        var act = () => sut.DeleteProjectAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
        conn.LastTransaction!.RolledBack.Should().BeTrue();
    }

    private static Project BuildProject()
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Book",
            Description = "Desc",
            GoalUnit = GoalUnit.Words,
            WordCountGoal = 50000,
            GoalAmount = 50000,
            StartDate = DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow,
            CurrentWordCount = 0,
            IsPublic = false
        };
    }
}
