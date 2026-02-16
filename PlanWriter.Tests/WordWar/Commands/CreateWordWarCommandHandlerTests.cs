using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Commands;
using PlanWriter.Application.WordWar.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Dtos.WordWars;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;
using Xunit;

namespace PlanWriter.Tests.WordWar.Commands;

public class CreateWordWarCommandHandlerTests
{
    private readonly Mock<ILogger<CreateWordWarCommandHandler>> _loggerMock = new();
    private readonly Mock<IEventReadRepository> _eventReadRepositoryMock = new();
    private readonly Mock<IWordWarReadRepository> _wordWarReadRepositoryMock = new();
    private readonly Mock<IWordWarRepository> _wordWarRepositoryMock = new();

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenDurationIsInvalid()
    {
        var command = NewCommand(durationMinutes: 0);
        var handler = CreateHandler();

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("DurationMinutes must be greater than 0");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var command = NewCommand();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDto?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Event not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenEventIsInactive()
    {
        var command = NewCommand();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                command.EventId,
                "Event",
                "event",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                50000,
                false));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("The event is no longer active.");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenEventIsOutsideValidPeriod()
    {
        var command = NewCommand();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                command.EventId,
                "Event",
                "event",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                50000,
                true));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("The event is outside the valid period.");
    }

    [Theory]
    [InlineData(WordWarStatus.Waiting)]
    [InlineData(WordWarStatus.Running)]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenAnotherWarIsActive(WordWarStatus status)
    {
        var command = NewCommand();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                command.EventId,
                "Event",
                "event",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                50000,
                true));

        _wordWarReadRepositoryMock
            .Setup(r => r.GetActiveByEventIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = Guid.NewGuid(),
                EventId = command.EventId,
                Status = status
            });

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("There is already a word war pending or in progress.");
    }

    [Fact]
    public async Task Handle_ShouldCreateWordWar_WhenRequestIsValid()
    {
        var command = NewCommand(durationMinutes: 15);
        var createdId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        _eventReadRepositoryMock
            .Setup(r => r.GetEventByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                command.EventId,
                "Event",
                "event",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                50000,
                true));

        _wordWarReadRepositoryMock
            .Setup(r => r.GetActiveByEventIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarsDto?)null);

        _wordWarRepositoryMock
            .Setup(r => r.CreateAsync(
                command.EventId,
                command.RequestedByUserId,
                command.DurationMinutes,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                WordWarStatus.Waiting,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdId);

        var handler = CreateHandler();
        var result = await handler.Handle(command, cts.Token);

        result.Should().Be(createdId);
        _wordWarRepositoryMock.Verify(r =>
            r.CreateAsync(
                command.EventId,
                command.RequestedByUserId,
                command.DurationMinutes,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                WordWarStatus.Waiting,
                cts.Token), Times.Once);
    }

    private CreateWordWarCommandHandler CreateHandler()
    {
        return new CreateWordWarCommandHandler(
            _loggerMock.Object,
            _eventReadRepositoryMock.Object,
            _wordWarReadRepositoryMock.Object,
            _wordWarRepositoryMock.Object);
    }

    private static CreateWordWarCommand NewCommand(int durationMinutes = 10)
    {
        return new CreateWordWarCommand(Guid.NewGuid(), durationMinutes, Guid.NewGuid());
    }
}
