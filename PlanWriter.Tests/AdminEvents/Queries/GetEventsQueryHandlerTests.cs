using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Application.AdminEvents.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Queries;

public class GetEventsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenRepositoryReturnsEmptyList()
    {
        // Arrange
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<GetEventQueryHandler>>();

        repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<EventDto>());

        var handler = new GetEventQueryHandler(
            repositoryMock.Object,
            loggerMock.Object
        );

        var query = new GetEventsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        repositoryMock.Verify(
            r => r.GetAllAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnEvents_WhenRepositoryReturnsEvents()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var events = new List<EventDto>
        {
            new EventDto(
                Guid.NewGuid(),
                "Evento 1",
                "evento-1",
                "nano",
                now.AddDays(-5),
                now.AddDays(10),
                50000,
                true
            ),
            new EventDto(
                Guid.NewGuid(),
                "Evento 2",
                "evento-2",
                "custom",
                now.AddDays(-1),
                now.AddDays(20),
                null,
                true
            )
        };

        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<GetEventQueryHandler>>();

        repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(events);

        var handler = new GetEventQueryHandler(
            repositoryMock.Object,
            loggerMock.Object
        );

        var query = new GetEventsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(events);

        repositoryMock.Verify(
            r => r.GetAllAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenRepositoryReturnsNull()
    {
        // Arrange
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<GetEventQueryHandler>>();

        repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync((List<EventDto>?)null);

        var handler = new GetEventQueryHandler(
            repositoryMock.Object,
            loggerMock.Object
        );

        var query = new GetEventsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        repositoryMock.Verify(
            r => r.GetAllAsync(),
            Times.Once
        );
    }
}