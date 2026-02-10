using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Commands;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories.Events.Admin;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Commands;

public class DeleteAdminEventCommandHandlerTests
{
    private readonly Mock<IAdminEventRepository> _repositoryMock = new();
    private readonly Mock<IAdminEventReadRepository> _repositorReadyMock = new();
    private readonly Mock<ILogger<DeleteAdminEventCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldDeleteEvent_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var existingEvent = new EventDto
        (
              eventId,
            "Evento para deletar",
            "evento-para-deletar",
            EventType.Nanowrimo.ToString(),
            DateTime.Now,
            DateTime.Now,
              100,
              true
              
        );

        _repositorReadyMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        _repositoryMock
            .Setup(r => r.DeleteAsync(existingEvent, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new DeleteAdminEventCommand(eventId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _repositorReadyMock.Verify(
            r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _repositoryMock.Verify(
            r => r.DeleteAsync(existingEvent, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _repositorReadyMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDto?)null);

        var handler = CreateHandler();
        var command = new DeleteAdminEventCommand(eventId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Event  not found");

        _repositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _repositorReadyMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var handler = CreateHandler();
        var command = new DeleteAdminEventCommand(eventId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("DB error");

        _repositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    /* ===================== HELPERS ===================== */

    private DeleteAdminEventCommandHandler CreateHandler()
    {
        return new DeleteAdminEventCommandHandler(
            _repositorReadyMock.Object,
            _repositoryMock.Object,
            _loggerMock.Object
        );
    }
}