using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Application.Events.Queries.Admin;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Events.Queries;

public class GetAdminEventByIdQueryHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<ILogger<GetAdminEventByIdQueryHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldReturnEventDto_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var ev = new Event
        {
            Id = eventId,
            Name = "Evento Teste",
            Slug = "evento-teste",
            Type = EventType.Nanowrimo,
            StartsAtUtc = DateTime.UtcNow.AddDays(-1),
            EndsAtUtc = DateTime.UtcNow.AddDays(10),
            DefaultTargetWords = 50000,
            IsActive = true
        };

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(ev);

        var handler = CreateHandler();
        var query = new GetAdminEventByIdQuery(eventId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new EventDto(
            ev.Id,
            ev.Name,
            ev.Slug,
            ev.Type.ToString(),
            ev.StartsAtUtc,
            ev.EndsAtUtc,
            ev.DefaultTargetWords,
            ev.IsActive
        ));
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventRepositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync((Event?)null);

        var handler = CreateHandler();
        var query = new GetAdminEventByIdQuery(eventId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /* ===================== HELPERS ===================== */

    private GetAdminEventByIdQueryHandler CreateHandler()
    {
        return new GetAdminEventByIdQueryHandler(
            _eventRepositoryMock.Object,
            _loggerMock.Object
        );
    }
}