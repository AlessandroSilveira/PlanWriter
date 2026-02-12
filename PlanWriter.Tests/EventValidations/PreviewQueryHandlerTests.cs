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
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.EventValidations;

public class PreviewQueryHandlerTests
{

    private readonly Mock<IEventReadRepository> _eventReadRepoMock = new();
    private readonly Mock<IProjectRepository> _projectRepoMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepoMock = new();
    private readonly Mock<ILogger<PreviewQueryHandler>> _loggerMock = new();
    private readonly Mock<IProjectProgressReadRepository> _progressReadRepoMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepoMock = new();

    private PreviewQueryHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _progressReadRepoMock.Object,
            _projectEventsReadRepoMock.Object,
            _eventReadRepoMock.Object,
            _projectReadRepoMock.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnTargetAndTotal_WhenContextIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var ev = new EventDto(
            Guid.NewGuid(),
            "Evento Antigo",
            "evento-antigo",
            "Custom",
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(10),
            10000,
            false
        );

        var projectEvent = new ProjectEvent
        {
            ProjectId = projectId,
            EventId = eventId,
            TargetWords = null
        };

        var progress = new List<ProjectProgress>
        {
            new() { ProjectId = projectId, WordsWritten = 1000, CreatedAt = now.AddDays(-5) },
            new() { ProjectId = projectId, WordsWritten = 2000, CreatedAt = now.AddDays(-1) }
        };

        _eventReadRepoMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);

        _projectReadRepoMock
            .Setup(r => r.GetProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto
            {
                Id = projectId
            });

        _projectEventsReadRepoMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        _progressReadRepoMock
            .Setup(r => r.GetProgressByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var handler = CreateHandler();
        var query = new PreviewQuery(userId, eventId, projectId);

        // Act
        var (target, total) =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        target.Should().Be(10000);
        total.Should().Be(3000);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        var handler = CreateHandler();
        var query = new PreviewQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _eventReadRepoMock
            .Setup(r => r.GetEventByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDto?)null);

        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Evento não encontrado.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProjectIsNotFound()
    {
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
                DateTime.UtcNow.AddDays(10),
                50000,
                true
            ));

        _projectReadRepoMock
            .Setup(r => r.GetProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectDto?)null);

        var handler = CreateHandler();
        var query = new PreviewQuery(userId, eventId, projectId);

        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Projeto não encontrado ou não pertence ao usuário.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProjectIsNotEnrolledInEvent()
    {
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
                DateTime.UtcNow.AddDays(10),
                50000,
                true
            ));

        _projectReadRepoMock
            .Setup(r => r.GetProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto
            {
                Id = projectId
            });

        _projectEventsReadRepoMock
            .Setup(r => r.GetByProjectAndEventWithEventAsync(projectId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectEvent?)null);

        var handler = CreateHandler();
        var query = new PreviewQuery(userId, eventId, projectId);

        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Projeto não está inscrito neste evento.");
    }
}
