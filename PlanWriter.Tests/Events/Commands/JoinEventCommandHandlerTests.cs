using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Events.Commands;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Events.Commands;

public class JoinEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IProjectEventsRepository> _projectEventsRepositoryMock = new();
    private readonly Mock<ILogger<JoinEventCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldCreateProjectEvent_WhenNotAlreadyJoined()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var ev = new Event
        {
            Id = eventId,
            DefaultTargetWords = 50000
        };

        var project = new Project
        {
            Id = projectId
        };

        var pe = new ProjectEvent
        {
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = 50000
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(ev);

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(project);

        _projectEventsRepositoryMock
            .Setup(r => r.GetProjectEventByProjectIdAndEventId(projectId, eventId))
            .ReturnsAsync((ProjectEvent?)null);

        _projectEventsRepositoryMock
            .Setup(r => r.AddProjectEvent(It.IsAny<ProjectEvent>()))
            .ReturnsAsync(pe);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(pe);

        _projectEventsRepositoryMock.Verify(
            r => r.AddProjectEvent(It.Is<ProjectEvent>(x =>
                x.ProjectId == projectId &&
                x.EventId == eventId &&
                x.TargetWords == 50000)),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnExistingProjectEvent_WhenAlreadyJoined_WithoutUpdatingTarget()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var ev = new Event
        {
            Id = eventId,
            DefaultTargetWords = 50000
        };

        var project = new Project { Id = projectId };

        var existing = new ProjectEvent
        {
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = 50000
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(ev);

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(project);

        _projectEventsRepositoryMock
            .Setup(r => r.GetProjectEventByProjectIdAndEventId(projectId, eventId))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existing);

        _projectEventsRepositoryMock.Verify(
            r => r.UpdateProjectEvent(It.IsAny<ProjectEvent>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldUpdateTargetWords_WhenAlreadyJoined_AndTargetDiffers()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var ev = new Event
        {
            Id = eventId,
            DefaultTargetWords = 50000
        };

        var project = new Project { Id = projectId };

        var existing = new ProjectEvent
        {
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = 30000
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(ev);

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(project);

        _projectEventsRepositoryMock
            .Setup(r => r.GetProjectEventByProjectIdAndEventId(projectId, eventId))
            .ReturnsAsync(existing);

        _projectEventsRepositoryMock
            .Setup(r => r.UpdateProjectEvent(existing))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = CreateCommand(projectId, eventId, 60000);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existing);
        existing.TargetWords.Should().Be(60000);

        _projectEventsRepositoryMock.Verify(
            r => r.UpdateProjectEvent(existing),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        var handler = CreateHandler();
        var command = CreateCommand(Guid.NewGuid(), Guid.NewGuid(), null);

        _eventRepositoryMock
            .Setup(r => r.GetEventById(It.IsAny<Guid>()))
            .ReturnsAsync((Event?)null);

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

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(new Event { Id = eventId });

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync((Project?)null);

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
            _loggerMock.Object
        );
    }

    private static JoinEventCommand CreateCommand(
        Guid projectId,
        Guid eventId,
        int? targetWords)
    {
        return new JoinEventCommand(
            new JoinEventRequest(
                projectId,
                eventId,
                targetWords
            )
        );
    }
}