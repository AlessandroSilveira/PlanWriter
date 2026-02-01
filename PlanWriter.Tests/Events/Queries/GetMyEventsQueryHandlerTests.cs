using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
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
        result.Should().HaveCount(2);
        result[0].Percent.Should().Be(50); // 25000 / 50000
        result[1].Percent.Should().Be(0);  // targetWords = 0
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

    

    private GetMyEventsQueryHandler CreateHandler()
    {
        return new GetMyEventsQueryHandler(_eventRepositoryMock.Object, _loggerMock.Object);
    }
}