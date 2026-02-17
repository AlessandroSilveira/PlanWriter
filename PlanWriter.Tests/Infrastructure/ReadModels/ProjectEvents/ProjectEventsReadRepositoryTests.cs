using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.ProjectEvents;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.ProjectEvents;

public class ProjectEventsReadRepositoryTests
{
    [Fact]
    public async Task GetByProjectAndEventWithEventAsync_ShouldReturnNull_WhenNoRow()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<ProjectEventWithEventRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectEventWithEventRow?)null);

        var sut = new ProjectEventsReadRepository(db.Object);
        var result = await sut.GetByProjectAndEventWithEventAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMostRecentWinByUserIdAsync_ShouldMapEventData()
    {
        var row = new ProjectEventWithEventRow(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            EventId: Guid.NewGuid(),
            TargetWords: 50000,
            Won: true,
            ValidatedAtUtc: DateTime.UtcNow,
            FinalWordCount: 52000,
            ValidatedWords: 52000,
            ValidationSource: "manual",
            EventName: "NaNo",
            EventSlug: "nano",
            EventType: 0,
            StartsAtUtc: DateTime.UtcNow.AddDays(-10),
            EndsAtUtc: DateTime.UtcNow.AddDays(10),
            DefaultTargetWords: 50000,
            IsActive: true
        );

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<ProjectEventWithEventRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(row);

        var sut = new ProjectEventsReadRepository(db.Object);
        var result = await sut.GetMostRecentWinByUserIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Won.Should().BeTrue();
        result.Event!.Name.Should().Be("NaNo");
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldMapRows()
    {
        var rows = new[]
        {
            new ProjectEventWithEventRow(
                Id: Guid.NewGuid(),
                ProjectId: Guid.NewGuid(),
                EventId: Guid.NewGuid(),
                TargetWords: 50000,
                Won: false,
                ValidatedAtUtc: null,
                FinalWordCount: null,
                ValidatedWords: null,
                ValidationSource: null,
                EventName: "NaNo",
                EventSlug: "nano",
                EventType: 0,
                StartsAtUtc: DateTime.UtcNow.AddDays(-10),
                EndsAtUtc: DateTime.UtcNow.AddDays(10),
                DefaultTargetWords: 50000,
                IsActive: true
            )
        };

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProjectEventWithEventRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new ProjectEventsReadRepository(db.Object);
        var result = await sut.GetByUserIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Event!.Slug.Should().Be("nano");
    }

    [Fact]
    public async Task GetByIdWithEventAsync_ShouldMapSingleRow()
    {
        var row = new ProjectEventWithEventRow(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            EventId: Guid.NewGuid(),
            TargetWords: 40000,
            Won: true,
            ValidatedAtUtc: DateTime.UtcNow,
            FinalWordCount: 42000,
            ValidatedWords: 42000,
            ValidationSource: "manual",
            EventName: "Camp",
            EventSlug: "camp",
            EventType: 1,
            StartsAtUtc: DateTime.UtcNow.AddDays(-10),
            EndsAtUtc: DateTime.UtcNow.AddDays(10),
            DefaultTargetWords: 40000,
            IsActive: true
        );

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<ProjectEventWithEventRow>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(row);

        var sut = new ProjectEventsReadRepository(db.Object);
        var result = await sut.GetByIdWithEventAsync(row.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Event!.Name.Should().Be("Camp");
        result.TargetWords.Should().Be(40000);
    }
}
