using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Commands;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Commands;

public class CreateEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _repositoryMock = new();
    private readonly Mock<ILogger<CreateEventCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldCreateEvent_WhenSlugIsUnique()
    {
        // Arrange
        var command = BuildCommand(
            name: "Meu Evento Legal",
            type: "Nano",
            defaultTarget: 50000
        );

        _repositoryMock
            .Setup(r => r.GetEventBySlug("meu-evento-legal"))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddEvent(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Meu Evento Legal");
        result.Slug.Should().Be("meu-evento-legal");
        result.Type.Should().Be(EventType.Nanowrimo.ToString());
        result.DefaultTargetWords.Should().Be(50000);
        result.IsActive.Should().BeTrue();

        _repositoryMock.Verify(
            r => r.AddEvent(It.Is<Event>(e =>
                e.Name == "Meu Evento Legal" &&
                e.Slug == "meu-evento-legal" &&
                e.Type == EventType.Nanowrimo
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSlugAlreadyExists()
    {
        // Arrange
        var command = BuildCommand(name: "Evento Duplicado");

        _repositoryMock
            .Setup(r => r.GetEventBySlug("evento-duplicado"))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Slug already in use: evento-duplicado");

        _repositoryMock.Verify(
            r => r.AddEvent(It.IsAny<Event>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldFallbackToNanowrimo_WhenEventTypeIsInvalid()
    {
        // Arrange
        var command = BuildCommand(
            name: "Evento Estranho",
            type: "TipoInvalido"
        );

        _repositoryMock
            .Setup(r => r.GetEventBySlug("evento-estranho"))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddEvent(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Type.Should().Be(EventType.Nanowrimo.ToString());
    }

    [Fact]
    public async Task Handle_ShouldNormalizeSlug_Correctly()
    {
        // Arrange
        var command = BuildCommand(name: "Evento. Com, Pontos");

        _repositoryMock
            .Setup(r => r.GetEventBySlug("evento-com-pontos"))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddEvent(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Slug.Should().Be("evento-com-pontos");
    }

    /* ===================== HELPERS ===================== */

    private CreateEventCommandHandler CreateHandler()
    {
        return new CreateEventCommandHandler(
            _repositoryMock.Object,
            _loggerMock.Object
        );
    }

    private static CreateEventCommand BuildCommand(
        string name,
        string type = "Nanowrimo",
        int? defaultTarget = null)
    {
        return new CreateEventCommand(
            new CreateEventRequest
            (
                name,
                type,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(30),
                defaultTarget
            )
        );
    }
}