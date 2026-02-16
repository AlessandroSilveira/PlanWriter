using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Commands;
using PlanWriter.Application.WordWar.Dtos.Commands;
using PlanWriter.Domain.Dtos.WordWars;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;
using Xunit;

namespace PlanWriter.Tests.WordWar.Commands;

public class SubmitWordWarCheckpointCommandHandlerTests
{
    private readonly Mock<ILogger<SubmitWordWarCheckpointCommandHandler>> _loggerMock = new();
    private readonly Mock<IWordWarReadRepository> _wordWarReadRepositoryMock = new();
    private readonly Mock<IWordWarRepository> _wordWarRepositoryMock = new();
    private readonly Mock<IWordWarParticipantReadRepository> _wordWarParticipantReadRepositoryMock = new();

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenWordsInRoundIsNegative()
    {
        var command = NewCommand(wordsInRound: -1);
        var handler = CreateHandler();

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("WordsInRound must be greater than or equal to 0.");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenWordWarDoesNotExist()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarsDto?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("WordWar not exist.");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenWordWarStatusIsNotRunning()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting,
                EndsAtUtc = DateTime.UtcNow.AddMinutes(10)
            });

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("It's only possible to create a checkpoint when the word war is running.");
    }

    [Fact]
    public async Task Handle_ShouldAutoFinishAndThrow_WhenRoundTimeHasEnded()
    {
        var command = NewCommand();
        using var cts = new CancellationTokenSource();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running,
                EndsAtUtc = DateTime.UtcNow.AddSeconds(-1)
            });

        _wordWarRepositoryMock
            .Setup(r => r.FinishAsync(command.WarId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _wordWarRepositoryMock
            .Setup(r => r.PersistFinalRankAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, cts.Token);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Word war has been auto-finished by time. Checkpoint rejected.");

        _wordWarRepositoryMock.Verify(
            r => r.FinishAsync(command.WarId, It.IsAny<DateTime>(), cts.Token),
            Times.Once);
        _wordWarRepositoryMock.Verify(
            r => r.SubmitCheckpointAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserIsNotParticipant()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running,
                EndsAtUtc = DateTime.UtcNow.AddMinutes(10)
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarParticipantsDto?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("The user is not participating in this word war.");
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenWordsInRoundIsEqualToCurrentValue()
    {
        var command = NewCommand(wordsInRound: 120);

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running,
                EndsAtUtc = DateTime.UtcNow.AddMinutes(10)
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                WordWarId = command.WarId,
                UserId = command.UserId,
                WordsInRound = 120
            });

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        _wordWarRepositoryMock.Verify(
            r => r.SubmitCheckpointAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenWordsInRoundIsLowerThanCurrentValue()
    {
        var command = NewCommand(wordsInRound: 119);

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running,
                EndsAtUtc = DateTime.UtcNow.AddMinutes(10)
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                WordWarId = command.WarId,
                UserId = command.UserId,
                WordsInRound = 120
            });

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("WordsInRound cannot be lower than the previous value.");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenRepositoryDoesNotPersistCheckpoint()
    {
        var command = NewCommand(wordsInRound: 121);

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running,
                EndsAtUtc = DateTime.UtcNow.AddMinutes(10)
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                WordWarId = command.WarId,
                UserId = command.UserId,
                WordsInRound = 120
            });

        _wordWarRepositoryMock
            .Setup(r => r.SubmitCheckpointAsync(command.WarId, command.UserId, command.WordsInRound, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Unable to persist checkpoint due to state conflict.");
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenRepositoryReturnsZeroButCheckpointAlreadyAdvancedConcurrently()
    {
        var command = NewCommand(wordsInRound: 121);

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running,
                EndsAtUtc = DateTime.UtcNow.AddMinutes(10)
            });

        _wordWarParticipantReadRepositoryMock
            .SetupSequence(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                WordWarId = command.WarId,
                UserId = command.UserId,
                WordsInRound = 120
            })
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                WordWarId = command.WarId,
                UserId = command.UserId,
                WordsInRound = 130
            });

        _wordWarRepositoryMock
            .Setup(r => r.SubmitCheckpointAsync(command.WarId, command.UserId, command.WordsInRound, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenCheckpointIsPersisted()
    {
        var command = NewCommand(wordsInRound: 121);
        using var cts = new CancellationTokenSource();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running,
                EndsAtUtc = DateTime.UtcNow.AddMinutes(10)
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                WordWarId = command.WarId,
                UserId = command.UserId,
                WordsInRound = 120
            });

        _wordWarRepositoryMock
            .Setup(r => r.SubmitCheckpointAsync(command.WarId, command.UserId, command.WordsInRound, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var result = await handler.Handle(command, cts.Token);

        result.Should().BeTrue();
        _wordWarRepositoryMock.Verify(
            r => r.SubmitCheckpointAsync(command.WarId, command.UserId, command.WordsInRound, It.IsAny<DateTime>(), cts.Token),
            Times.Once);
    }

    private SubmitWordWarCheckpointCommandHandler CreateHandler()
    {
        return new SubmitWordWarCheckpointCommandHandler(
            _loggerMock.Object,
            _wordWarReadRepositoryMock.Object,
            _wordWarRepositoryMock.Object,
            _wordWarParticipantReadRepositoryMock.Object);
    }

    private static SubmitWordWarCheckpointCommand NewCommand(int wordsInRound = 100)
    {
        return new SubmitWordWarCheckpointCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            wordsInRound);
    }
}
