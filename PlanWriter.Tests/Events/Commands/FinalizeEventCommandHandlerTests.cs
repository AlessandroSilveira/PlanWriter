using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Events.Commands;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Events.Commands;

public class FinalizeEventCommandHandlerTests
{
    private readonly Mock<IProjectEventsRepository> _projectEventsRepoMock = new();
    private readonly Mock<IEventRepository> _eventRepoMock = new();
    private readonly Mock<IProjectProgressRepository> _projectProgressRepoMock = new();
    private readonly Mock<IBadgeRepository> _badgeRepoMock = new();
    private readonly Mock<ILogger<FinalizeEventCommandHandler>> _loggerMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepoMock = new();

    private FinalizeEventCommandHandler CreateHandler()
        => new(
            _projectEventsRepoMock.Object,
            _eventRepoMock.Object,
            _projectProgressRepoMock.Object,
            _badgeRepoMock.Object,
            _loggerMock.Object,
            _projectEventsReadRepoMock.Object
        );

    [Fact]
    public async Task Handle_ShouldFinalizeEventAndMarkAsWinner_WhenTargetIsReached()
    {
        // Arrange
        var projectEventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var ev = new Event
        {
            Id = eventId,
            Name = "NaNoWriMo",
            StartsAtUtc = DateTime.UtcNow.AddDays(-30),
            EndsAtUtc = DateTime.UtcNow
        };

        var projectEvent = new ProjectEvent
        {
            Id = projectEventId,
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = 50000,
            Event = ev
        };

        var progressEntries = new List<ProjectProgress>
        {
            new() { ProjectId = projectId, WordsWritten = 30000 },
            new() { ProjectId = projectId, WordsWritten = 25000 }
        };

        _projectEventsReadRepoMock
            .Setup(r => r.GetByIdWithEventAsync(projectEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        _projectProgressRepoMock
            .Setup(r => r.GetByProjectAndDateRangeAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(progressEntries);

        _projectEventsRepoMock
            .Setup(r => r.UpdateProjectEvent(projectEvent, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _badgeRepoMock
            .Setup(r => r.SaveAsync(It.IsAny<List<Badge>>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new FinalizeEventCommand(
            new FinalizeRequest (projectEventId )
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Won.Should().BeTrue();
        result.FinalWordCount.Should().Be(55000);
        result.ValidatedWords.Should().Be(55000);
        result.ValidatedAtUtc.Should().NotBeNull();

        _projectEventsRepoMock.Verify(
            r => r.UpdateProjectEvent(projectEvent, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _badgeRepoMock.Verify(
            r => r.SaveAsync(It.Is<List<Badge>>(b =>
                b.Count == 1 &&
                b[0].Icon == "🏆")),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldFinalizeEventAndMarkAsParticipant_WhenTargetIsNotReached()
    {
        // Arrange
        var projectEventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var ev = new Event
        {
            Id = eventId,
            Name = "NaNoWriMo",
            StartsAtUtc = DateTime.UtcNow.AddDays(-10),
            EndsAtUtc = DateTime.UtcNow
        };

        var projectEvent = new ProjectEvent
        {
            Id = projectEventId,
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = 50000,
            Event = ev
        };

        var progressEntries = new List<ProjectProgress>
        {
            new() { ProjectId = projectId, WordsWritten = 10000 }
        };

        _projectEventsReadRepoMock
            .Setup(r => r.GetByIdWithEventAsync(projectEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        _projectProgressRepoMock
            .Setup(r => r.GetByProjectAndDateRangeAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(progressEntries);

        _projectEventsRepoMock
            .Setup(r => r.UpdateProjectEvent(projectEvent, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _badgeRepoMock
            .Setup(r => r.SaveAsync(It.IsAny<List<Badge>>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new FinalizeEventCommand(
            new FinalizeRequest (projectEventId )
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Won.Should().BeFalse();
        result.FinalWordCount.Should().Be(10000);

        _badgeRepoMock.Verify(
            r => r.SaveAsync(It.Is<List<Badge>>(b =>
                b.Count == 1 &&
                b[0].Icon == "🎉")),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProjectEventDoesNotExist()
    {
        // Arrange
        var projectEventId = Guid.NewGuid();

        _projectEventsReadRepoMock
            .Setup(r => r.GetByIdWithEventAsync(projectEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectEvent?)null);

        var handler = CreateHandler();
        var command = new FinalizeEventCommand(
            new FinalizeRequest (projectEventId )
        );

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Inscrição não encontrada.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        // Arrange
        var projectEventId = Guid.NewGuid();
        var projectEvent = new ProjectEvent
        {
            Id = projectEventId,
            ProjectId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Event = null
        };

        _projectEventsReadRepoMock
            .Setup(r => r.GetByIdWithEventAsync(projectEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        _eventRepoMock
            .Setup(r => r.GetEventById(projectEvent.EventId))
            .ReturnsAsync((Event?)null);

        var handler = CreateHandler();
        var command = new FinalizeEventCommand(
            new FinalizeRequest (projectEventId )
        );

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Evento não encontrado.");
    }
}
