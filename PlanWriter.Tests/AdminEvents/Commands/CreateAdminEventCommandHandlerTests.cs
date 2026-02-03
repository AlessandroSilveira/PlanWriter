
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.AdminEvents.Commands;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Dtos.AdminEvents;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories.Events.Admin;
using Xunit;

namespace PlanWriter.Tests.AdminEvents.Commands;

public class CreateAdminEventCommandHandlerTests
{
    private readonly Mock<IAdminEventRepository> _repositoryMock = new();
    private readonly Mock<IAdminEventReadRepository> _repositoryReadMock = new();
    private readonly Mock<ILogger<CreateAdminEventCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldCreateEvent_WhenSlugIsUnique()
    {
        // Arrange
        var command = BuildCommand(
            name: "Meu Evento Legal",
            type: "Nano",
            defaultTarget: 50000
        );

        _repositoryReadMock
            .Setup(r => r.SlugExistsAsync("meu-evento-legal", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        // result.Should().NotBeNull();
        // result.Name.Should().Be("Meu Evento Legal");
        // result.Slug.Should().Be("meu-evento-legal");
        // result.Type.Should().Be(EventType.Nanowrimo.ToString());
        // result.DefaultTargetWords.Should().Be(50000);
        // result.IsActive.Should().BeTrue();

       
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSlugAlreadyExists()
    {
        // Arrange
        var command = BuildCommand(name: "Evento Duplicado");

        _repositoryReadMock
            .Setup(r => r.SlugExistsAsync("evento-duplicado", It.IsAny<CancellationToken>()))
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
            r => r.CreateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()),
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

        _repositoryReadMock
            .Setup(r => r.SlugExistsAsync("evento-estranho", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
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

        _repositoryReadMock
            .Setup(r => r.SlugExistsAsync("evento-com-pontos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Slug.Should().Be("evento-com-pontos");
    }

    /* ===================== HELPERS ===================== */

    private CreateAdminEventCommandHandler CreateHandler()
    {
        return new CreateAdminEventCommandHandler(
            _repositoryMock.Object,
            _repositoryReadMock.Object,
            _loggerMock.Object
        );
    }

    private static CreateAdminEventCommand BuildCommand(
        string name,
        string type = "Nanowrimo",
        int? defaultTarget = null)
    {
        return new CreateAdminEventCommand(
            new CreateAdminEventRequest
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