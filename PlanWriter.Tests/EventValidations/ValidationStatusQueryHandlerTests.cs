using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.EventValidation.Dtos.Queries;
using PlanWriter.Application.EventValidation.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.EventValidations;

public class ValidationStatusQueryHandlerTests
{
    private readonly Mock<IEventReadRepository> _eventReadRepoMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepoMock = new();
    private readonly Mock<ILogger<ValidationStatusQueryHandler>> _loggerMock = new();
    private readonly Mock<IProjectProgressReadRepository> _progressReadRepoMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepoMock = new();

    private ValidationStatusQueryHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _progressReadRepoMock.Object,
            _projectEventsReadRepoMock.Object,
            _eventReadRepoMock.Object,
            _projectReadRepoMock.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnCanValidateTrue_WhenInsideWindowAndTargetReached()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _eventReadRepoMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-3),
                DateTime.UtcNow.AddDays(3),
                1000,
                true,
                DateTime.UtcNow.AddHours(-1),
                DateTime.UtcNow.AddHours(1),
                "current,paste"
            ));

        _projectReadRepoMock
            .Setup(r => r.GetProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto { Id = projectId });

        _projectEventsReadRepoMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EventId = eventId,
                TargetWords = 1000,
                Won = false,
                ValidatedAtUtc = null
            });

        _progressReadRepoMock
            .Setup(r => r.GetProgressByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectProgress>
            {
                new() { ProjectId = projectId, WordsWritten = 400, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new() { ProjectId = projectId, WordsWritten = 700, CreatedAt = DateTime.UtcNow.AddHours(-2) }
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ValidationStatusQuery(userId, eventId, projectId), CancellationToken.None);

        // Assert
        result.TargetWords.Should().Be(1000);
        result.TotalWords.Should().Be(1100);
        result.IsValidated.Should().BeFalse();
        result.IsWithinValidationWindow.Should().BeTrue();
        result.CanValidate.Should().BeTrue();
        result.BlockReason.Should().BeNull();
        result.AllowedSources.Should().BeEquivalentTo(new[] { "current", "paste" });
    }

    [Fact]
    public async Task Handle_ShouldReturnBlocked_WhenOutsideWindow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _eventReadRepoMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(10),
                1000,
                true,
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(-1),
                "current,paste,manual"
            ));

        _projectReadRepoMock
            .Setup(r => r.GetProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto { Id = projectId });

        _projectEventsReadRepoMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EventId = eventId,
                TargetWords = 1000,
                Won = false
            });

        _progressReadRepoMock
            .Setup(r => r.GetProgressByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectProgress>
            {
                new() { ProjectId = projectId, WordsWritten = 1500, CreatedAt = DateTime.UtcNow.AddDays(-2) }
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ValidationStatusQuery(userId, eventId, projectId), CancellationToken.None);

        // Assert
        result.IsWithinValidationWindow.Should().BeFalse();
        result.CanValidate.Should().BeFalse();
        result.BlockReason.Should().Be("Validação fora da janela permitida.");
    }

    [Fact]
    public async Task Handle_ShouldReturnBlocked_WhenAlreadyValidated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _eventReadRepoMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                1000,
                true
            ));

        _projectReadRepoMock
            .Setup(r => r.GetProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto { Id = projectId });

        _projectEventsReadRepoMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EventId = eventId,
                TargetWords = 1000,
                Won = true,
                ValidatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                ValidatedWords = 1200,
                ValidationSource = "manual"
            });

        _progressReadRepoMock
            .Setup(r => r.GetProgressByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectProgress>
            {
                new() { ProjectId = projectId, WordsWritten = 1200, CreatedAt = DateTime.UtcNow.AddHours(-3) }
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ValidationStatusQuery(userId, eventId, projectId), CancellationToken.None);

        // Assert
        result.IsValidated.Should().BeTrue();
        result.CanValidate.Should().BeFalse();
        result.BlockReason.Should().Be("Projeto já validado neste evento.");
        result.ValidatedWords.Should().Be(1200);
        result.ValidationSource.Should().Be("manual");
    }
}
