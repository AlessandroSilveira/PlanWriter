using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Events;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Application.Events.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Events.Queries;

public class GetMyEventsQueryHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<ILogger<GetMyEventsQueryHandler>> _loggerMock = new();
    private readonly Mock<IEventLifecycleService> _eventLifecycleServiceMock = new();
    private readonly IEventProgressCalculator _eventProgressCalculator = new EventProgressCalculator();

    [Fact]
    public async Task Handle_ShouldReturnEventsWithCalculatedPercent_WhenEventsExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        var events = new List<MyEventDto>
        {
            new MyEventDto
            {
                EventId = Guid.NewGuid(),
                TargetWords = 50000,
                TotalWrittenInEvent = 25000
            },
            new MyEventDto
            {
                EventId = Guid.NewGuid(),
                TargetWords = 0,
                TotalWrittenInEvent = 1000
            },
            new MyEventDto
            {
                EventId = Guid.NewGuid(),
                TargetWords = null,
                TotalWrittenInEvent = null
            },
            new MyEventDto
            {
                EventId = Guid.NewGuid(),
                TargetWords = 0,
                EventDefaultTargetWords = 40000,
                TotalWrittenInEvent = 1000
            }
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventByUserId(userId))
            .ReturnsAsync(events);

        var handler = CreateHandler();
        var query = new GetMyEventsQuery(userId);

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().HaveCount(4);
        result[0].Percent.Should().Be(50); // 25000 / 50000
        result[1].Percent.Should().Be(2);  // targetWords=0 -> fallback 50000
        result[2].Percent.Should().Be(0);  // targetWords/totalWritten null -> fallback 50000
        result[3].Percent.Should().Be(3);  // targetWords=0 -> fallback event default (40000)
        result[0].Won.Should().BeFalse();
        result[1].Won.Should().BeFalse();
        result[2].Won.Should().BeFalse();
        result[3].Won.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _eventRepositoryMock
            .Setup(r => r.GetEventByUserId(userId))
            .ReturnsAsync(new List<MyEventDto>());

        var handler = CreateHandler();
        var query = new GetMyEventsQuery(userId);

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldAnnotateEffectiveStatus_ForLifecycleScenarios()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var events = new List<MyEventDto>
        {
            new()
            {
                EventId = Guid.NewGuid(),
                EventIsActive = true,
                StartsAtUtc = now.AddHours(-1),
                EndsAtUtc = now.AddHours(1),
                TargetWords = 100,
                TotalWrittenInEvent = 10
            },
            new()
            {
                EventId = Guid.NewGuid(),
                EventIsActive = true,
                StartsAtUtc = now.AddHours(1),
                EndsAtUtc = now.AddHours(2),
                TargetWords = 100,
                TotalWrittenInEvent = 0
            },
            new()
            {
                EventId = Guid.NewGuid(),
                EventIsActive = true,
                StartsAtUtc = now.AddHours(-2),
                EndsAtUtc = now.AddHours(-1),
                TargetWords = 100,
                TotalWrittenInEvent = 50
            },
            new()
            {
                EventId = Guid.NewGuid(),
                EventIsActive = false,
                StartsAtUtc = now.AddHours(-1),
                EndsAtUtc = now.AddHours(1),
                TargetWords = 100,
                TotalWrittenInEvent = 20
            }
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventByUserId(userId))
            .ReturnsAsync(events);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyEventsQuery(userId), CancellationToken.None);

        result.Select(r => r.EffectiveStatus).Should().ContainInOrder("active", "scheduled", "closed", "disabled");
        result[0].IsEffectivelyActive.Should().BeTrue();
        result[1].IsEffectivelyActive.Should().BeFalse();
        result[2].IsEffectivelyActive.Should().BeFalse();
        result[3].IsEffectivelyActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldPreferPersistedFinalSnapshot_ForClosedValidatedEvents()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var closedValidatedEvent = new MyEventDto
        {
            EventId = Guid.NewGuid(),
            EventIsActive = true,
            StartsAtUtc = now.AddDays(-10),
            EndsAtUtc = now.AddDays(-1),
            TargetWords = 1000,
            TotalWrittenInEvent = 1500, // live sum can drift after closure
            FinalWordCountSnapshot = 900,
            ValidatedWordsSnapshot = 850,
            PersistedWon = false,
            ValidatedAtUtc = now.AddHours(-2)
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventByUserId(userId))
            .ReturnsAsync(new List<MyEventDto> { closedValidatedEvent });

        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyEventsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].EffectiveStatus.Should().Be("closed");
        result[0].TotalWrittenInEvent.Should().Be(850); // prefers ValidatedWords snapshot
        result[0].Percent.Should().Be(85);
        result[0].Won.Should().BeFalse(); // persisted winner result must win over recalculation
    }

    private GetMyEventsQueryHandler CreateHandler()
    {
        return new GetMyEventsQueryHandler(
            _eventRepositoryMock.Object,
            _eventProgressCalculator,
            _eventLifecycleServiceMock.Object,
            _loggerMock.Object);
    }
}
