using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Application.Events.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;
using GetActiveEventsQueryHandler = PlanWriter.Application.AdminEvents.Queries.GetActiveEventsQueryHandler;

namespace PlanWriter.Tests.Events.Queries;

public class GetActiveEventsQueryHandlerTests
{
    private readonly Mock<IEventReadRepository> _eventRepositoryMock = new();
    private readonly Mock<ILogger<GetActiveEventsQueryHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldReturnActiveEvents_WhenEventsExist()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new(
                Guid.NewGuid(),
                "Evento 1",
                "evento-1",
                "Nano",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(10),
                50000,
                true
            ),
            new(
                Guid.NewGuid(),
                "Evento 2",
                "evento-2",
                "Custom",
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(20),
                null,
                true
            )
        };

        _eventRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var handler = CreateHandler();
        var query = new GetActiveEventsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(events);

        _eventRepositoryMock.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoActiveEventsExist()
    {
        // Arrange
        _eventRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventDto>());

        var handler = CreateHandler();
        var query = new GetActiveEventsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _eventRepositoryMock.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /* ===================== HELPERS ===================== */

    private GetActiveEventsQueryHandler CreateHandler()
    {
        return new GetActiveEventsQueryHandler(_eventRepositoryMock.Object, _loggerMock.Object);
    }
}