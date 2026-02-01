using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.EventValidation.Dtos.Queries;
using PlanWriter.Application.EventValidation.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.EventValidations;

public class PreviewQueryHandlerTests
{
    private readonly Mock<IProjectProgressRepository> _progressRepoMock = new();
    private readonly Mock<IEventRepository> _eventRepoMock = new();
    private readonly Mock<IProjectRepository> _projectRepoMock = new();
    private readonly Mock<IProjectEventsRepository> _projectEventsRepoMock = new();
    private readonly Mock<ILogger<PreviewQueryHandler>> _loggerMock = new();

    private PreviewQueryHandler CreateHandler()
        => new(
            _progressRepoMock.Object,
            _eventRepoMock.Object,
            _projectRepoMock.Object,
            _projectEventsRepoMock.Object,
            _loggerMock.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnTargetAndTotal_WhenContextIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var ev = new Event
        {
            Id = eventId,
            StartsAtUtc = now.AddDays(-10),
            EndsAtUtc = now.AddDays(10),
            DefaultTargetWords = 50000
        };

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

        _eventRepoMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(ev);

        _projectRepoMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId });

        _projectEventsRepoMock
            .Setup(r => r.GetProjectEventByProjectIdAndEventId(projectId, eventId))
            .ReturnsAsync(projectEvent);

        _progressRepoMock
            .Setup(r => r.GetProgressByProjectIdAsync(projectId, userId))
            .ReturnsAsync(progress);

        var handler = CreateHandler();
        var query = new PreviewQuery(userId, eventId, projectId);

        // Act
        var (target, total) =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        target.Should().Be(50000);
        total.Should().Be(3000);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        var handler = CreateHandler();
        var query = new PreviewQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _eventRepoMock
            .Setup(r => r.GetEventById(It.IsAny<Guid>()))
            .ReturnsAsync((Event?)null);

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

        _eventRepoMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(new Event());

        _projectRepoMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync((Project?)null);

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

        _eventRepoMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(new Event());

        _projectRepoMock
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId });

        _projectEventsRepoMock
            .Setup(r => r.GetProjectEventByProjectIdAndEventId(projectId, eventId))
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