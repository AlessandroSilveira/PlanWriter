using System.Collections;
using System.Reflection;
using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;
using PlanWriter.Tests.Infrastructure;
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
