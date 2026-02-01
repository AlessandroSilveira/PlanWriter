using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Commands;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Commands;

public class DeleteEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _repositoryMock = new();
    private readonly Mock<ILogger<DeleteEventCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldDeleteEvent_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var existingEvent = new Event
        {
            Id = eventId,
            Name = "Evento para deletar",
            Slug = "evento-para-deletar",
            Type = EventType.Nanowrimo,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync(existingEvent);

        _repositoryMock
            .Setup(r => r.DeleteAsync(existingEvent))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new DeleteEventCommand(eventId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _repositoryMock.Verify(
            r => r.GetEventById(eventId),
            Times.Once
        );

        _repositoryMock.Verify(
            r => r.DeleteAsync(existingEvent),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ReturnsAsync((Event?)null);

        var handler = CreateHandler();
        var command = new DeleteEventCommand(eventId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Event  not found");

        _repositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<Event>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetEventById(eventId))
            .ThrowsAsync(new Exception("DB error"));

        var handler = CreateHandler();
        var command = new DeleteEventCommand(eventId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("DB error");

        _repositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<Event>()),
            Times.Never
        );
    }

    /* ===================== HELPERS ===================== */

    private DeleteEventCommandHandler CreateHandler()
    {
        return new DeleteEventCommandHandler(
            _repositoryMock.Object,
            _loggerMock.Object
        );
    }
}