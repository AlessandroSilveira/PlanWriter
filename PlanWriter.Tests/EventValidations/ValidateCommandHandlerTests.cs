using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.EventValidation.Commands;
using PlanWriter.Application.EventValidation.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.EventValidations;

public class ValidateCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IProjectEventsRepository> _projectEventsRepositoryMock = new();
    private readonly Mock<ILogger<ValidateCommandHandler>> _loggerMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepositoryMock = new();
    private readonly Mock<IEventReadRepository> _eventReadRepositoryMock = new();

    private ValidateCommandHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _projectRepositoryMock.Object,
            _projectEventsRepositoryMock.Object,
            _projectEventsReadRepositoryMock.Object,
            _eventReadRepositoryMock.Object
        );

    [Fact]
    public async Task Handle_ShouldValidateProjectEvent_WhenWordsMeetTarget()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var ev = new Event
        {
            Id = eventId,
            DefaultTargetWords = 50000
        };

        var projectEvent = new ProjectEvent
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = null // usa DefaultTargetWords
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(ev);

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        _projectEventsRepositoryMock
            .Setup(r => r.UpdateProjectEvent(projectEvent, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new ValidateCommand(userId, eventId, projectId, 52000, "manual");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        projectEvent.Won.Should().BeTrue();
        projectEvent.ValidatedWords.Should().Be(52000);
        projectEvent.ValidatedAtUtc.Should().NotBeNull();
        projectEvent.ValidationSource.Should().Be("manual");

        _projectEventsRepositoryMock.Verify(
            r => r.UpdateProjectEvent(projectEvent, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenWordsAreBelowTarget()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var ev = new Event
        {
            Id = eventId,
            DefaultTargetWords = 50000
        };

        var projectEvent = new ProjectEvent
        {
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = null
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(ev);

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        var command = new ValidateCommand(userId, eventId, projectId, 1000, "manual");
      

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Total informado (1000) é menor que a meta (50000).");

        _projectEventsRepositoryMock.Verify(
            r => r.UpdateProjectEvent(It.IsAny<ProjectEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        // Arrange
        var command = new ValidateCommand(Guid.NewGuid(),  Guid.NewGuid(),  Guid.NewGuid(),50000, "manual");
       
        _eventRepositoryMock
            .Setup(r => r.GetEventById(It.IsAny<Guid>()))
            .ReturnsAsync((Event?)null);

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Evento não encontrado.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProjectDoesNotExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(new Event { Id = eventId });

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync((Project?)null);

        var command = new ValidateCommand(Guid.NewGuid(),  eventId,  projectId,50000, "manual");
     
        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Projeto não encontrado ou não pertence ao usuário.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProjectIsNotEnrolledInEvent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(new Event { Id = eventId });

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectEvent?)null);

        var command = new ValidateCommand(Guid.NewGuid(),  eventId,  projectId,50000, "manual");

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Projeto não está inscrito neste evento.");
    }
}