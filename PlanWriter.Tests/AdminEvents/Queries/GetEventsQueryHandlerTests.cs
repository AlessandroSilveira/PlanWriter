using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Application.AdminEvents.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Repositories.Events.Admin;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Queries;

public class GetEventsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenRepositoryReturnsEmptyList()
    {
        // Arrange
        var repositoryMock = new Mock<IAdminEventRepository>();
        var repositoryReadMock = new Mock<IAdminEventReadRepository>();
        var loggerMock = new Mock<ILogger<GetAdminEventQueryHandler>>();

        repositoryReadMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventDto>());

        var handler = new GetAdminEventQueryHandler(
            repositoryReadMock.Object,
            loggerMock.Object
        );

        var query = new GetAdminEventsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        repositoryReadMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
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

        var repositoryMock = new Mock<IAdminEventReadRepository>();
        var loggerMock = new Mock<ILogger<GetAdminEventQueryHandler>>();

        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var handler = new GetAdminEventQueryHandler(
            repositoryMock.Object,
            loggerMock.Object
        );

        var query = new GetAdminEventsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(events);

        repositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenRepositoryReturnsNull()
    {
        // Arrange
        var repositoryMock = new Mock<IAdminEventReadRepository>();
        var loggerMock = new Mock<ILogger<GetAdminEventQueryHandler>>();

        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<EventDto>?)null);

        var handler = new GetAdminEventQueryHandler(
            repositoryMock.Object,
            loggerMock.Object
        );

        var query = new GetAdminEventsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        repositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}