using FluentAssertions;
using MediatR;
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

public class FinishWordWarCommandHandlerTests
{
    private readonly Mock<ILogger<FinishWordWarCommandHandler>> _loggerMock = new();
    private readonly Mock<IWordWarReadRepository> _wordWarReadRepositoryMock = new();
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
            r => r.FinishAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenStatusIsNotRunning()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Only word wars in running status can be finished.");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenFinishAffectsZeroRows()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running
            });

        _wordWarRepositoryMock
            .Setup(r => r.FinishAsync(command.WarId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Word War não está em execução.");

        _wordWarRepositoryMock.Verify(
            r => r.PersistFinalRankAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnit_WhenFinishAffectsZeroButWarIsAlreadyFinished()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .SetupSequence(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running
            })
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Finished
            });

        _wordWarRepositoryMock
            .Setup(r => r.FinishAsync(command.WarId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _wordWarRepositoryMock.Verify(
            r => r.PersistFinalRankAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFinishWordWar_AndPersistFinalRank()
    {
        var command = NewCommand();
        using var cts = new CancellationTokenSource();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running
            });

        _wordWarRepositoryMock
            .Setup(r => r.FinishAsync(command.WarId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _wordWarRepositoryMock
            .Setup(r => r.PersistFinalRankAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var result = await handler.Handle(command, cts.Token);

        result.Should().Be(Unit.Value);
        _wordWarRepositoryMock.Verify(
            r => r.FinishAsync(command.WarId, It.IsAny<DateTime>(), cts.Token),
            Times.Once);
        _wordWarRepositoryMock.Verify(
            r => r.PersistFinalRankAsync(command.WarId, cts.Token),
            Times.Once);
    }

    private FinishWordWarCommandHandler CreateHandler()
    {
        return new FinishWordWarCommandHandler(
            _loggerMock.Object,
            _wordWarReadRepositoryMock.Object,
            _wordWarRepositoryMock.Object);
    }

    private static FinishWordWarCommand NewCommand()
    {
        return new FinishWordWarCommand(Guid.NewGuid(), Guid.NewGuid());
    }
}
