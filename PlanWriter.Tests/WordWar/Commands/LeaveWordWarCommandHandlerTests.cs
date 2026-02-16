using FluentAssertions;
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

public class LeaveWordWarCommandHandlerTests
{
    private readonly Mock<ILogger<LeaveWordWarCommandHandler>> _loggerMock = new();
    private readonly Mock<IWordWarReadRepository> _wordWarReadRepositoryMock = new();
    private readonly Mock<IWordWarParticipantReadRepository> _wordWarParticipantReadRepositoryMock = new();
    private readonly Mock<IWordWarRepository> _wordWarRepositoryMock = new();

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

        _wordWarRepositoryMock.Verify(
            r => r.LeaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenWordWarStatusIsNotWaiting()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running
            });

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Can't leave the word war when the status is not waiting.");

        _wordWarParticipantReadRepositoryMock.Verify(
            r => r.GetParticipant(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenUserIsNotParticipant()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarParticipantsDto?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        _wordWarRepositoryMock.Verify(
            r => r.LeaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenLeaveAffectsOneRow()
    {
        var command = NewCommand();
        using var cts = new CancellationTokenSource();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                Id = Guid.NewGuid(),
                WordWarId = command.WarId,
                UserId = command.UserId,
                ProjectId = Guid.NewGuid()
            });

        _wordWarRepositoryMock
            .Setup(r => r.LeaveAsync(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var result = await handler.Handle(command, cts.Token);

        result.Should().BeTrue();
        _wordWarRepositoryMock.Verify(
            r => r.LeaveAsync(command.WarId, command.UserId, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenLeaveDoesNotAffectRowsButUserIsAlreadyAbsent()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        _wordWarParticipantReadRepositoryMock
            .SetupSequence(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                Id = Guid.NewGuid(),
                WordWarId = command.WarId,
                UserId = command.UserId,
                ProjectId = Guid.NewGuid()
            })
            .ReturnsAsync((EventWordWarParticipantsDto?)null);

        _wordWarRepositoryMock
            .Setup(r => r.LeaveAsync(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenLeaveDoesNotAffectRowsAndUserStillPresent()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                Id = Guid.NewGuid(),
                WordWarId = command.WarId,
                UserId = command.UserId,
                ProjectId = Guid.NewGuid()
            });

        _wordWarRepositoryMock
            .Setup(r => r.LeaveAsync(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Unable to leave word war due to state conflict.");
    }

    private LeaveWordWarCommandHandler CreateHandler()
    {
        return new LeaveWordWarCommandHandler(
            _loggerMock.Object,
            _wordWarReadRepositoryMock.Object,
            _wordWarParticipantReadRepositoryMock.Object,
            _wordWarRepositoryMock.Object);
    }

    private static LeaveWordWarCommand NewCommand()
    {
        return new LeaveWordWarCommand(
            Guid.NewGuid(),
            Guid.NewGuid());
    }
}
