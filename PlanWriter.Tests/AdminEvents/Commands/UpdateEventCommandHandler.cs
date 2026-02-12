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
using PlanWriter.Domain.Requests;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Commands;

public class UpdateAdminEventCommandHandlerTests
{
    private readonly Mock<IAdminEventRepository> _repositoryMock = new();
    private readonly Mock<IAdminEventReadRepository> _repositoryReadMock = new();
    private readonly Mock<ILogger<UpdateAdminEventCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldUpdateEvent_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var existingEvent = new EventDto
        (
            eventId,
            "Evento Antigo",
            "evento-antigo",
            EventType.Nanowrimo.ToString(),
            now.AddDays(-10),
            now.AddDays(10),
            50000,
            true
        );

        var command = BuildCommand(
            eventId,
            name: "Evento Atualizado",
            type: "Nano",
            isActive: false
        );

        _repositoryReadMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        EventDto? updatedEvent = null;
        _repositoryMock
            .Setup(r => r.UpdateAsync(eventId, It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, EventDto, CancellationToken>((_, ev, _) => updatedEvent = ev)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        updatedEvent.Should().NotBeNull();
        updatedEvent!.Name.Should().Be("Evento Atualizado");
        updatedEvent.Slug.Should().Be("evento-atualizado");
        updatedEvent.Type.Should().Be(EventType.Nanowrimo.ToString());
        updatedEvent.IsActive.Should().BeFalse();

        _repositoryMock.Verify(
            r => r.UpdateAsync(eventId, It.IsAny<EventDto>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var command = BuildCommand(eventId);

        _repositoryReadMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDto?)null);

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("Error updating event");

        _repositoryMock.Verify(
            r => r.UpdateAsync( It.IsAny<Guid>(), It.IsAny<EventDto>(),It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldFallbackToNanowrimo_WhenTypeIsInvalid()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var existingEvent = new EventDto
        (
            eventId,
            "Evento",
            "evento",
            EventType.Nanowrimo.ToString(),
            now.AddDays(-5),
            now.AddDays(5),
            100,
            true
        );

        var command = BuildCommand(
            eventId,
            name: "Evento Novo",
            type: "TipoInvalido"
        );

        _repositoryReadMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        EventDto? updatedEvent = null;
        _repositoryMock
            .Setup(r => r.UpdateAsync(eventId, It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, EventDto, CancellationToken>((_, ev, _) => updatedEvent = ev)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        updatedEvent.Should().NotBeNull();
        updatedEvent!.Type.Should().Be(EventType.Nanowrimo.ToString());
    }

    [Fact]
    public async Task Handle_ShouldThrowGenericException_WhenRepositoryThrows()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var command = BuildCommand(eventId);

        _repositoryReadMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
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

    private UpdateAdminEventCommandHandler CreateHandler()
    {
        return new UpdateAdminEventCommandHandler(
            _repositoryReadMock.Object,
            _repositoryMock.Object,
            _loggerMock.Object
        );
    }

    private static UpdateAdminEventCommand BuildCommand(
        Guid eventId,
        string name = "Evento Atualizado",
        string type = "Nanowrimo",
        bool isActive = true)
    {
        return new UpdateAdminEventCommand(
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
