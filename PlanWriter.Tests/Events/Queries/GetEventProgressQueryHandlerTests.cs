using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Events;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Application.Events.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Events.Queries;

public class GetEventProgressQueryHandlerTests
{
    private readonly Mock<IProjectProgressRepository> _projectProgressRepoMock = new();
    private readonly Mock<ILogger<GetEventProgressQueryHandler>> _loggerMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectProgressReadRepoMock = new();
    private readonly IEventProgressCalculator _eventProgressCalculator = new EventProgressCalculator();
    

    [Fact]
    public async Task Handle_ShouldReturnProgress_WhenProjectEventExists()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var ev = new PlanWriter.Domain.Events.Event
        {
            Id = eventId,
            Name = "NaNoWriMo",
            StartsAtUtc = now.AddDays(-10),
            EndsAtUtc = now.AddDays(20),
            DefaultTargetWords = 50000
        };

        var projectEvent = new PlanWriter.Domain.Events.ProjectEvent
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = null,
            Event = ev,
            Won = false
        };

        var entries = new List<ProjectProgress>
        {
            new() { ProjectId = projectId, WordsWritten = 1000, CreatedAt = now.AddDays(-2) },
            new() { ProjectId = projectId, WordsWritten = 2000, CreatedAt = now.AddDays(-1) }
        };

        _projectProgressReadRepoMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        _projectProgressRepoMock
            .Setup(r => r.GetByProjectAndDateRangeAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var handler = new GetEventProgressQueryHandler(
            _projectProgressRepoMock.Object,
            _loggerMock.Object,
            _projectProgressReadRepoMock.Object,
            _eventProgressCalculator
        );

        var query = new GetEventProgressQuery(eventId, projectId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalWrittenInEvent.Should().Be(3000);
        result.Percent.Should().Be(6);
        result.Won.Should().BeFalse();
        result.TargetWords.Should().Be(50000);
        result.Name.Should().Be("NaNoWriMo");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProjectEventDoesNotExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _projectProgressReadRepoMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlanWriter.Domain.Events.ProjectEvent?)null);

        var handler = new GetEventProgressQueryHandler(
            _projectProgressRepoMock.Object,
            _loggerMock.Object,
            _projectProgressReadRepoMock.Object,
            _eventProgressCalculator
        );

        var query = new GetEventProgressQuery(eventId, projectId);

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Inscrição do projeto no evento não encontrada.");
    }

    [Fact]
    public async Task Handle_ShouldCalculateWon_WhenTotalIsGreaterThanOrEqualToTarget()
    {
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var ev = new PlanWriter.Domain.Events.Event
        {
            Id = eventId,
            Name = "Sprint Event",
            StartsAtUtc = now.AddDays(-5),
            EndsAtUtc = now.AddDays(5),
            DefaultTargetWords = 1000
        };

        var projectEvent = new PlanWriter.Domain.Events.ProjectEvent
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = 1000,
            Event = ev,
            Won = false
        };

        _projectProgressReadRepoMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        _projectProgressRepoMock
            .Setup(r => r.GetByProjectAndDateRangeAsync(projectId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProjectProgress { ProjectId = projectId, WordsWritten = 1200, CreatedAt = now.AddDays(-1) }
            });

        var handler = new GetEventProgressQueryHandler(
            _projectProgressRepoMock.Object,
            _loggerMock.Object,
            _projectProgressReadRepoMock.Object,
            _eventProgressCalculator
        );

        var result = await handler.Handle(new GetEventProgressQuery(eventId, projectId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Won.Should().BeTrue();
        result.Percent.Should().Be(120);
        result.Remaining.Should().Be(0);
    }
}
