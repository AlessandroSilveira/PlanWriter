using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Events;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Events.Common;

public class EventLifecycleServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepositoryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<EventLifecycleService>> _loggerMock = new();

    [Fact]
    public async Task SyncExpiredEventsAsync_ShouldCloseExpiredActiveEvents_AndFinalizeParticipants()
    {
        var eventId = Guid.NewGuid();
        var expired = new EventDto(eventId, "Evento", "evento", "Nanowrimo", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddMinutes(-1), 50000, true);
        var eventEntity = new Event
        {
            Id = eventId,
            Name = expired.Name,
            Slug = expired.Slug,
            Type = EventType.Nanowrimo,
            StartsAtUtc = expired.StartsAtUtc,
            EndsAtUtc = expired.EndsAtUtc,
            DefaultTargetWords = expired.DefaultTargetWords,
            IsActive = true
        };
        var participants = new List<ProjectEvent>
        {
            new() { Id = Guid.NewGuid(), EventId = eventId, ProjectId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), EventId = eventId, ProjectId = Guid.NewGuid() }
        };

        _eventRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<EventDto> { expired });
        _eventRepositoryMock.Setup(x => x.GetEventById(eventId)).ReturnsAsync(eventEntity);
        _projectEventsReadRepositoryMock.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(participants);
        _mediatorMock.Setup(x => x.Send(It.IsAny<FinalizeEventCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ProjectEvent());

        var sut = CreateSut();
        await sut.SyncExpiredEventsAsync(CancellationToken.None);

        eventEntity.IsActive.Should().BeFalse();
        _eventRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Event>(e => e.Id == eventId && e.IsActive == false), eventId), Times.Once);
        _mediatorMock.Verify(x => x.Send(It.IsAny<FinalizeEventCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SyncEventIfExpiredAsync_ShouldIgnoreEvent_WhenStillActiveWithinWindow()
    {
        var eventId = Guid.NewGuid();
        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Evento",
            Slug = "evento",
            Type = EventType.Nanowrimo,
            StartsAtUtc = DateTime.UtcNow.AddDays(-1),
            EndsAtUtc = DateTime.UtcNow.AddDays(1),
            IsActive = true
        };

        _eventRepositoryMock.Setup(x => x.GetEventById(eventId)).ReturnsAsync(eventEntity);

        var sut = CreateSut();
        await sut.SyncEventIfExpiredAsync(eventId, CancellationToken.None);

        _eventRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>(), It.IsAny<Guid>()), Times.Never);
        _mediatorMock.Verify(x => x.Send(It.IsAny<FinalizeEventCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncEventIfExpiredAsync_ShouldIgnoreEvent_WhenAlreadyInactive()
    {
        var eventId = Guid.NewGuid();
        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Evento",
            Slug = "evento",
            Type = EventType.Nanowrimo,
            StartsAtUtc = DateTime.UtcNow.AddDays(-10),
            EndsAtUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = false
        };

        _eventRepositoryMock.Setup(x => x.GetEventById(eventId)).ReturnsAsync(eventEntity);

        var sut = CreateSut();
        await sut.SyncEventIfExpiredAsync(eventId, CancellationToken.None);

        _eventRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>(), It.IsAny<Guid>()), Times.Never);
        _mediatorMock.Verify(x => x.Send(It.IsAny<FinalizeEventCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private EventLifecycleService CreateSut()
        => new(_eventRepositoryMock.Object, _projectEventsReadRepositoryMock.Object, _mediatorMock.Object, _loggerMock.Object);
}
