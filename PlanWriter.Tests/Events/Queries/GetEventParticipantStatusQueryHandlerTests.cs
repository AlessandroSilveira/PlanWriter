using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Events;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Common.WinnerEligibility;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Application.Events.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.Events.Queries;

public class GetEventParticipantStatusQueryHandlerTests
{
    private readonly Mock<ILogger<GetEventParticipantStatusQueryHandler>> _loggerMock = new();
    private readonly Mock<IEventReadRepository> _eventReadRepositoryMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepositoryMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepositoryMock = new();
    private readonly Mock<IProjectProgressReadRepository> _projectProgressReadRepositoryMock = new();
    private readonly Mock<IEventLifecycleService> _eventLifecycleServiceMock = new();
    private readonly IEventProgressCalculator _eventProgressCalculator = new EventProgressCalculator();
    private readonly IWinnerEligibilityService _winnerEligibilityService = new WinnerEligibilityService();

    [Fact]
    public async Task Handle_ShouldReturnReadyToValidate_WhenTargetReachedWithinValidationWindow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _eventReadRepositoryMock
            .Setup(x => x.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento de Fevereiro",
                "evento-fev",
                "Nanowrimo",
                now.AddDays(-10),
                now.AddDays(10),
                50000,
                true,
                now.AddDays(-1),
                now.AddDays(1),
                "current,paste"));

        _projectReadRepositoryMock
            .Setup(x => x.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId, Title = "Meu Romance" });

        _projectEventsReadRepositoryMock
            .Setup(x => x.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EventId = eventId,
                TargetWords = 50000,
                Won = false,
                ValidatedAtUtc = null
            });

        _projectProgressReadRepositoryMock
            .Setup(x => x.GetProgressByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectProgress>
            {
                new() { ProjectId = projectId, WordsWritten = 25000, CreatedAt = now.AddDays(-2) },
                new() { ProjectId = projectId, WordsWritten = 25000, CreatedAt = now.AddHours(-2) }
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetEventParticipantStatusQuery(userId, eventId, projectId), CancellationToken.None);

        // Assert
        result.EventStatus.Should().Be("active");
        result.IsEventActive.Should().BeTrue();
        result.IsValidationWindowOpen.Should().BeTrue();
        result.TotalWords.Should().Be(50000);
        result.TargetWords.Should().Be(50000);
        result.Percent.Should().Be(100);
        result.IsEligible.Should().BeFalse();
        result.CanValidate.Should().BeTrue();
        result.ValidationBlockReason.Should().BeNull();
        result.EligibilityStatus.Should().Be("pending_validation");
        result.AllowedValidationSources.Should().BeEquivalentTo(new[] { "current", "paste" });
    }

    [Fact]
    public async Task Handle_ShouldUsePersistedSnapshot_WhenValidatedAndEventClosed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _eventReadRepositoryMock
            .Setup(x => x.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento Encerrado",
                "evento-encerrado",
                "Nanowrimo",
                now.AddDays(-30),
                now.AddDays(-5),
                1000,
                true));

        _projectReadRepositoryMock
            .Setup(x => x.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId, Title = "Projeto Vencedor" });

        _projectEventsReadRepositoryMock
            .Setup(x => x.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EventId = eventId,
                TargetWords = 1000,
                Won = true,
                ValidatedAtUtc = now.AddDays(-4),
                ValidatedWords = 1200,
                ValidationSource = "manual"
            });

        _projectProgressReadRepositoryMock
            .Setup(x => x.GetProgressByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectProgress>
            {
                new() { ProjectId = projectId, WordsWritten = 100, CreatedAt = now.AddDays(-10) }
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetEventParticipantStatusQuery(userId, eventId, projectId), CancellationToken.None);

        // Assert
        result.EventStatus.Should().Be("closed");
        result.IsEventClosed.Should().BeTrue();
        result.TotalWords.Should().Be(1200); // snapshot final prevalece após encerramento
        result.Percent.Should().Be(120);
        result.IsValidated.Should().BeTrue();
        result.IsWinner.Should().BeTrue();
        result.IsEligible.Should().BeTrue();
        result.CanValidate.Should().BeFalse();
        result.ValidationBlockReason.Should().Be("Projeto já validado neste evento.");
        result.EligibilityStatus.Should().Be("eligible");
    }

    [Fact]
    public async Task Handle_ShouldBlockValidation_WhenWindowIsClosed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _eventReadRepositoryMock
            .Setup(x => x.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                now.AddDays(-10),
                now.AddDays(10),
                500,
                true,
                now.AddDays(-5),
                now.AddDays(-1),
                "current,paste,manual"));

        _projectReadRepositoryMock
            .Setup(x => x.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId, Title = "Projeto X" });

        _projectEventsReadRepositoryMock
            .Setup(x => x.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EventId = eventId,
                TargetWords = 500,
                Won = false
            });

        _projectProgressReadRepositoryMock
            .Setup(x => x.GetProgressByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectProgress>
            {
                new() { ProjectId = projectId, WordsWritten = 600, CreatedAt = now.AddDays(-2) }
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetEventParticipantStatusQuery(userId, eventId, projectId), CancellationToken.None);

        // Assert
        result.TotalWords.Should().Be(600);
        result.EligibilityStatus.Should().Be("pending_validation");
        result.IsValidationWindowOpen.Should().BeFalse();
        result.CanValidate.Should().BeFalse();
        result.ValidationBlockReason.Should().Be("Validação fora da janela permitida.");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenProjectIsNotParticipant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _eventReadRepositoryMock
            .Setup(x => x.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                eventId,
                "Evento",
                "evento",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                50000,
                true));

        _projectReadRepositoryMock
            .Setup(x => x.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId, Title = "Projeto X" });

        _projectEventsReadRepositoryMock
            .Setup(x => x.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectEvent?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new GetEventParticipantStatusQuery(userId, eventId, projectId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Projeto não está inscrito neste evento.");
    }

    private GetEventParticipantStatusQueryHandler CreateHandler()
    {
        return new GetEventParticipantStatusQueryHandler(
            _loggerMock.Object,
            _eventReadRepositoryMock.Object,
            _projectReadRepositoryMock.Object,
            _projectEventsReadRepositoryMock.Object,
            _projectProgressReadRepositoryMock.Object,
            _eventProgressCalculator,
            _winnerEligibilityService,
            _eventLifecycleServiceMock.Object);
    }
}
