using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Events.Commands;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Events.Commands;

public class JoinEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IProjectEventsRepository> _projectEventsRepositoryMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepositoryMock = new();
    private readonly Mock<ILogger<JoinEventCommandHandler>> _loggerMock = new();
    private readonly Mock<IEventReadRepository> _eventReadRepositoryMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepositoryMock = new();
    

    [Fact]
    public async Task Handle_ShouldCreateProjectEvent_WhenNotAlreadyJoined()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEventDto(eventId, 50000));

        _projectReadRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto
            {
                Id = projectId
            });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectEvent?)null);

        _projectEventsRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<ProjectEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ProjectId.Should().Be(projectId);
        result.EventId.Should().Be(eventId);
        result.TargetWords.Should().Be(50000);

        _projectEventsRepositoryMock.Verify(
            r => r.CreateAsync(It.Is<ProjectEvent>(x =>
                x.ProjectId == projectId &&
                x.EventId == eventId &&
                x.TargetWords == 50000), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldFallbackToEventDefaultTarget_WhenRequestTargetIsNull()
    {
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEventDto(eventId, 42000));

        _projectReadRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectEvent?)null);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.TargetWords.Should().Be(42000);
    }

    [Fact]
    public async Task Handle_ShouldFallbackToGlobalDefaultTarget_WhenRequestAndEventDefaultAreNull()
    {
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEventDto(eventId, null));

        _projectReadRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectEvent?)null);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.TargetWords.Should().Be(50000);
        _projectEventsRepositoryMock.Verify(
            r => r.CreateAsync(It.Is<ProjectEvent>(x => x.TargetWords == 50000), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnExistingProjectEvent_WhenAlreadyJoined_WithoutUpdatingTarget()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var existing = new ProjectEvent
        {
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = 50000
        };

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEventDto(eventId, 50000));

        _projectReadRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto
            {
                Id = projectId
            });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existing);

        _projectEventsRepositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<ProjectEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldUpdateTargetWords_WhenAlreadyJoined_AndTargetDiffers()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var existing = new ProjectEvent
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = 30000
        };

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEventDto(eventId, 50000));

        _projectReadRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto
            {
                Id = projectId
            });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, 60000);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existing);
        existing.TargetWords.Should().Be(60000);

        _projectEventsRepositoryMock.Verify(
            r => r.UpdateTargetWordsAsync(existing.Id, 60000, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingTargetToFallback_WhenExistingAndRequestAreNull()
    {
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var existing = new ProjectEvent
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = null
        };

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEventDto(eventId, null));

        _projectReadRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.TargetWords.Should().Be(50000);
        _projectEventsRepositoryMock.Verify(
            r => r.UpdateTargetWordsAsync(existing.Id, 50000, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        var handler = CreateHandler();
        var command = CreateCommand(Guid.NewGuid(), Guid.NewGuid(), null);

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDto?)null);

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Evento não encontrado.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProjectDoesNotExist()
    {
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEventDto(eventId, 50000));

        _projectReadRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectDto?)null);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, null);

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Projeto não encontrado.");
    }

    /* ===================== HELPERS ===================== */

    private JoinEventCommandHandler CreateHandler()
    {
        return new JoinEventCommandHandler(
            _eventRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _projectEventsRepositoryMock.Object,
            _projectEventsReadRepositoryMock.Object,
            _loggerMock.Object,
            _eventReadRepositoryMock.Object,
            _projectReadRepositoryMock.Object
        );
    }

    private static JoinEventCommand CreateCommand(Guid projectId, Guid eventId, int? targetWords)
    {
        return new JoinEventCommand(
            new JoinEventRequest(
                projectId,
                eventId,
                targetWords
            ), Guid.NewGuid()
        );
    }

    private static EventDto CreateEventDto(Guid eventId, int? defaultTargetWords)
    {
        return new EventDto(
            eventId,
            "Evento",
            "evento",
            "Nanowrimo",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(10),
            defaultTargetWords,
            true
        );
    }
}
