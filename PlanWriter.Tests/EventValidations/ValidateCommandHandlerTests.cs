using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.EventValidation.Commands;
using PlanWriter.Application.EventValidation.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.EventValidations;

public class ValidateCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IProjectEventsRepository> _projectEventsRepositoryMock = new();
    private readonly Mock<ILogger<ValidateCommandHandler>> _loggerMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepositoryMock = new();
    private readonly Mock<IEventReadRepository> _eventReadRepositoryMock = new();
    private readonly Mock<IEventValidationAuditRepository> _eventValidationAuditRepositoryMock = new();

    private ValidateCommandHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _projectRepositoryMock.Object,
            _projectEventsRepositoryMock.Object,
            _projectEventsReadRepositoryMock.Object,
            _eventReadRepositoryMock.Object,
            _eventValidationAuditRepositoryMock.Object
        );

    [Fact]
    public async Task Handle_ShouldValidateProjectEvent_WhenWordsMeetTargetAndPolicyAllows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var projectEvent = new ProjectEvent
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = null
        };

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(10),
                50000,
                true,
                DateTime.UtcNow.AddHours(-1),
                DateTime.UtcNow.AddHours(1),
                "current,paste,manual"
            ));

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        _projectEventsRepositoryMock
            .Setup(r => r.UpdateProjectEvent(projectEvent, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventValidationAuditRepositoryMock
            .Setup(r => r.CreateAsync(
                eventId,
                projectId,
                userId,
                "manual",
                52000,
                "approved",
                It.IsAny<DateTime?>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new ValidateCommand(userId, eventId, projectId, 52000, "manual");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        projectEvent.Won.Should().BeTrue();
        projectEvent.ValidatedWords.Should().Be(52000);
        projectEvent.FinalWordCount.Should().Be(52000);
        projectEvent.ValidatedAtUtc.Should().NotBeNull();
        projectEvent.ValidationSource.Should().Be("manual");

        _projectEventsRepositoryMock.Verify(
            r => r.UpdateProjectEvent(projectEvent, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _eventValidationAuditRepositoryMock.Verify(
            r => r.CreateAsync(
                eventId,
                projectId,
                userId,
                "manual",
                52000,
                "approved",
                It.IsAny<DateTime?>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRule_WhenWordsAreBelowTarget()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var projectEvent = new ProjectEvent
        {
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = null
        };

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                50000,
                true,
                DateTime.UtcNow.AddHours(-1),
                DateTime.UtcNow.AddHours(1),
                "current,paste,manual"
            ));

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
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Total informado (1000) é menor que a meta (50000).");

        _projectEventsRepositoryMock.Verify(
            r => r.UpdateProjectEvent(It.IsAny<ProjectEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _eventValidationAuditRepositoryMock.Verify(
            r => r.CreateAsync(
                eventId,
                projectId,
                userId,
                "manual",
                1000,
                "rejected",
                null,
                "Total informado (1000) é menor que a meta (50000).",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRule_WhenSourceIsNotAllowed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                50000,
                true,
                DateTime.UtcNow.AddHours(-1),
                DateTime.UtcNow.AddHours(1),
                "current,paste"
            ));

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EventId = eventId,
                TargetWords = 100
            });

        var handler = CreateHandler();
        var command = new ValidateCommand(userId, eventId, projectId, 1000, "manual");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Fonte de validação não permitida para este evento.");

        _eventValidationAuditRepositoryMock.Verify(
            r => r.CreateAsync(
                eventId,
                projectId,
                userId,
                "manual",
                1000,
                "rejected",
                null,
                "Fonte de validação não permitida para este evento.",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRule_WhenOutsideValidationWindow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(10),
                50000,
                true,
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(-1),
                "current,paste,manual"
            ));

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EventId = eventId,
                TargetWords = 100
            });

        var handler = CreateHandler();
        var command = new ValidateCommand(userId, eventId, projectId, 1000, "manual");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Validação fora da janela permitida.*");

        _eventValidationAuditRepositoryMock.Verify(
            r => r.CreateAsync(
                eventId,
                projectId,
                userId,
                "manual",
                1000,
                "rejected",
                null,
                It.Is<string>(reason => reason != null && reason.StartsWith("Validação fora da janela permitida.")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        // Arrange
        var command = new ValidateCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50000, "manual");

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDto?)null);

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

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(10),
                50000,
                true
            ));

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync((Project?)null);

        var command = new ValidateCommand(Guid.NewGuid(), eventId, projectId, 50000, "manual");

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

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(10),
                50000,
                true
            ));

        _projectRepositoryMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId });

        _projectEventsReadRepositoryMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectEvent?)null);

        var command = new ValidateCommand(Guid.NewGuid(), eventId, projectId, 50000, "manual");

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
