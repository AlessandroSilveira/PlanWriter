using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Commands;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Requests;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Commands;

public class UpdateEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _repositoryMock = new();
    private readonly Mock<ILogger<UpdateEventCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldUpdateEvent_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var existingEvent = new Event
        {
            Id = eventId,
            Name = "Evento Antigo",
            Slug = "evento-antigo",
            Type = EventType.Nanowrimo,
            StartsAtUtc = now.AddDays(-10),
            EndsAtUtc = now.AddDays(10),
            IsActive = true,
            DefaultTargetWords = 50000
        };

        var command = BuildCommand(
            eventId,
            name: "Evento Atualizado",
            type: "Nano",
            isActive: false
        );

        _repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(existingEvent);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Event>(), eventId))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        existingEvent.Name.Should().Be("Evento Atualizado");
        existingEvent.Slug.Should().Be("evento-atualizado");
        existingEvent.Type.Should().Be(EventType.Nanowrimo);
        existingEvent.IsActive.Should().BeFalse();

        _repositoryMock.Verify(
            r => r.UpdateAsync(existingEvent, eventId),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var command = BuildCommand(eventId);

        _repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync((Event?)null);

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("Error updating event");

        _repositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldFallbackToNanowrimo_WhenTypeIsInvalid()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var existingEvent = new Event
        {
            Id = eventId,
            Name = "Evento",
            Slug = "evento",
            Type = EventType.Nanowrimo,
            StartsAtUtc = now.AddDays(-5),
            EndsAtUtc = now.AddDays(5),
            IsActive = true
        };

        var command = BuildCommand(
            eventId,
            name: "Evento Novo",
            type: "TipoInvalido"
        );

        _repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(existingEvent);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Event>(), eventId))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        existingEvent.Type.Should().Be(EventType.Nanowrimo);
    }

    [Fact]
    public async Task Handle_ShouldThrowGenericException_WhenRepositoryThrows()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var command = BuildCommand(eventId);

        _repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("Error updating event");
    }

    /* ===================== HELPERS ===================== */

    private UpdateEventCommandHandler CreateHandler()
    {
        return new UpdateEventCommandHandler(
            _repositoryMock.Object,
            _loggerMock.Object
        );
    }

    private static UpdateEventCommand BuildCommand(
        Guid eventId,
        string name = "Evento Atualizado",
        string type = "Nanowrimo",
        bool isActive = true)
    {
        return new UpdateEventCommand(
            new UpdateEventDto
            {
                Name = name,
                Type = type,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = isActive,
                TargetWords = 60000
            },   eventId
        );
    }
}