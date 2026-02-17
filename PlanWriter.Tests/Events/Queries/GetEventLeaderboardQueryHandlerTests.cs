using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Events;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Application.Events.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Events.Queries;

public class GetEventLeaderboardQueryHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<ILogger<GetEventLeaderboardQueryHandler>> _loggerMock = new();
    private readonly IEventProgressCalculator _eventProgressCalculator = new EventProgressCalculator();

    private GetEventLeaderboardQueryHandler CreateHandler()
        => new(_eventRepositoryMock.Object, _eventProgressCalculator, _loggerMock.Object);

    [Fact]
    public async Task Handle_ShouldReturnLeaderboard_ForGlobalScope()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var ev = new Event
        {
            Id = eventId,
            StartsAtUtc = now.AddDays(-10),
            EndsAtUtc = now.AddDays(10)
        };

        var leaderboardRows = new List<EventLeaderboardRowDto>
        {
            new() { ProjectTitle = "A", Words = 2000, TargetWords = 50000 },
            new() { ProjectTitle = "B", Words = 3000, TargetWords = 50000 }
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(ev);

        _eventRepositoryMock
            .Setup(r => r.GetLeaderboard(
                ev,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                10))
            .ReturnsAsync(leaderboardRows);

        var handler = CreateHandler();
        var query = new GetEventLeaderboardQuery(eventId, "all", 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Rank.Should().Be(1);
        result[1].Rank.Should().Be(2);
        result[0].Words.Should().Be(3000);
        result[0].Percent.Should().Be(6);
        result[0].Won.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_ForDailyScopeOutsideEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var ev = new Event
        {
            Id = eventId,
            StartsAtUtc = now.AddDays(1),
            EndsAtUtc = now.AddDays(10)
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(ev);

        var handler = CreateHandler();
        var query = new GetEventLeaderboardQuery(eventId, "daily", 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync((Event?)null);

        var handler = CreateHandler();
        var query = new GetEventLeaderboardQuery(eventId, "all", 10);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Evento não encontrado.");
    }
}
