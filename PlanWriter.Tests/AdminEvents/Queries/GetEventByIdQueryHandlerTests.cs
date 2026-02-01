using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Application.AdminEvents.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Queries;

public class GetEventByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEventDto_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var domainEvent = new Event
        {
            Id = eventId,
            Name = "Evento Teste",
            Slug = "evento-teste",
            Type = EventType.Nanowrimo,
            StartsAtUtc = now.AddDays(-1),
            EndsAtUtc = now.AddDays(10),
            DefaultTargetWords = 50000,
            IsActive = true
        };

        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<GetEventByIdQueryHandler>>();
        repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(domainEvent);

        var handler = new GetEventByIdQueryHandler(repositoryMock.Object, loggerMock.Object);
        var query = new GetEventByIdQuery(eventId);

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

        repositoryMock.Verify(
            r => r.GetEventById(eventId),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<GetEventByIdQueryHandler>>();
        repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync((Event?)null);

        var handler = new GetEventByIdQueryHandler(repositoryMock.Object, loggerMock.Object);
        var query = new GetEventByIdQuery(eventId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        repositoryMock.Verify(
            r => r.GetEventById(eventId),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<GetEventByIdQueryHandler>>();
        repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var handler = new GetEventByIdQueryHandler(repositoryMock.Object, loggerMock.Object);
        var query = new GetEventByIdQuery(eventId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");

        repositoryMock.Verify(
            r => r.GetEventById(eventId),
            Times.Once
        );
    }
}