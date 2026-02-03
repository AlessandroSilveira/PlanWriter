using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Application.AdminEvents.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Queries;

public class GetAdminEventByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEventDto_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var domainEvent = new EventDto
        (
            eventId,
            "Evento Teste",
            "evento-teste",
            "Nanowrimo",
            now.AddDays(-1),
            now.AddDays(10),
            50000,
            true
        );

        var repositoryReadMock = new Mock<IAdminEventReadRepository>();
        var loggerMock = new Mock<ILogger<GetAdminEventByIdQueryHandler>>();
        repositoryReadMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainEvent);

        var handler = new GetAdminEventByIdQueryHandler(repositoryReadMock.Object, loggerMock.Object);
        var query = new GetAdminEventByIdQuery(eventId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(
            new EventDto(
                Id: domainEvent.Id,
                Name: domainEvent.Name,
                Slug: domainEvent.Slug,
                Type: domainEvent.Type.ToString(),
                StartsAtUtc: domainEvent.StartsAtUtc,
                EndsAtUtc: domainEvent.EndsAtUtc,
                DefaultTargetWords: domainEvent.DefaultTargetWords,
                IsActive: domainEvent.IsActive
            )
        );

        repositoryReadMock.Verify(
            r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var repositoryReadMock = new Mock<IAdminEventReadRepository>();
        var loggerMock = new Mock<ILogger<GetAdminEventByIdQueryHandler>>();
        repositoryReadMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDto?)null);

        var handler = new GetAdminEventByIdQueryHandler(repositoryReadMock.Object, loggerMock.Object);
        var query = new GetAdminEventByIdQuery(eventId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        repositoryReadMock.Verify(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var repositoryReadMock = new Mock<IAdminEventReadRepository>();
        var loggerMock = new Mock<ILogger<GetAdminEventByIdQueryHandler>>();
        repositoryReadMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var handler = new GetAdminEventByIdQueryHandler(repositoryReadMock.Object, loggerMock.Object);
        var query = new GetAdminEventByIdQuery(eventId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");

        repositoryReadMock.Verify(
            r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}