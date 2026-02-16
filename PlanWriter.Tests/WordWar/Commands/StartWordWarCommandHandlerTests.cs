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

public class StartWordWarCommandHandlerTests
{
    private readonly Mock<ILogger<StartWordWarCommandHandler>> _loggerMock = new();
    private readonly Mock<IWordWarReadRepository> _wordWarReadRepositoryMock = new();
    private readonly Mock<IWordWarRepository> _wordWarRepositoryMock = new();

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenWordWarDoesNotExist()
    {
        var warId = Guid.NewGuid();
        var command = new StartWordWarCommand(warId, Guid.NewGuid());

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(warId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarsDto?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("WordWar not exist.");

        _wordWarRepositoryMock.Verify(
            r => r.StartAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenStatusIsNotWaiting()
    {
        var warId = Guid.NewGuid();
        var command = new StartWordWarCommand(warId, Guid.NewGuid());

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(warId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = warId,
                Status = WordWarStatus.Running,
                DurationInMinuts = 15
            });

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Only word wars in waiting status can be started.");

        _wordWarRepositoryMock.Verify(
            r => r.StartAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldStartWordWar_WhenStatusIsWaiting()
    {
        var warId = Guid.NewGuid();
        var command = new StartWordWarCommand(warId, Guid.NewGuid());
        var durationMinutes = 20;
        using var cts = new CancellationTokenSource();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(warId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = warId,
                Status = WordWarStatus.Waiting,
                DurationInMinuts = durationMinutes
            });

        DateTime capturedStartsAt = default;
        DateTime capturedEndsAt = default;
        CancellationToken capturedCt = default;

        _wordWarRepositoryMock
            .Setup(r => r.StartAsync(warId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, DateTime, DateTime, CancellationToken>((_, startsAt, endsAt, ct) =>
            {
                capturedStartsAt = startsAt;
                capturedEndsAt = endsAt;
                capturedCt = ct;
            })
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var result = await handler.Handle(command, cts.Token);

        result.Should().Be(Unit.Value);
        capturedStartsAt.Should().NotBe(default);
        capturedEndsAt.Should().Be(capturedStartsAt.AddMinutes(durationMinutes));
        capturedCt.Should().Be(cts.Token);

        _wordWarRepositoryMock.Verify(
            r => r.StartAsync(warId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenDurationIsInvalid()
    {
        var warId = Guid.NewGuid();
        var command = new StartWordWarCommand(warId, Guid.NewGuid());

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(warId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = warId,
                Status = WordWarStatus.Waiting,
                DurationInMinuts = 0
            });

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("WordWar has invalid duration.");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenStartReturnsZeroRows()
    {
        var warId = Guid.NewGuid();
        var command = new StartWordWarCommand(warId, Guid.NewGuid());

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(warId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = warId,
                Status = WordWarStatus.Waiting,
                DurationInMinuts = 10
            });

        _wordWarRepositoryMock
            .Setup(r => r.StartAsync(warId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("*A state conflict occurred while attempting to start the word war*");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnit_WhenStartReturnsZeroButWarIsAlreadyRunning()
    {
        var warId = Guid.NewGuid();
        var command = new StartWordWarCommand(warId, Guid.NewGuid());

        _wordWarReadRepositoryMock
            .SetupSequence(r => r.GetByIdAsync(warId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = warId,
                Status = WordWarStatus.Waiting,
                DurationInMinuts = 10
            })
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = warId,
                Status = WordWarStatus.Running,
                DurationInMinuts = 10
            });

        _wordWarRepositoryMock
            .Setup(r => r.StartAsync(warId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        var warId = Guid.NewGuid();
        var command = new StartWordWarCommand(warId, Guid.NewGuid());

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(warId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = warId,
                Status = WordWarStatus.Waiting,
                DurationInMinuts = 10
            });

        _wordWarRepositoryMock
            .Setup(r => r.StartAsync(warId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db error"));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("db error");
    }

    private StartWordWarCommandHandler CreateHandler()
    {
        return new StartWordWarCommandHandler(
            _loggerMock.Object,
            _wordWarReadRepositoryMock.Object,
            _wordWarRepositoryMock.Object);
    }
}
