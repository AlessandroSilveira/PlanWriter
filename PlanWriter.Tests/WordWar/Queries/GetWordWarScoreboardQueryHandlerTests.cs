using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Queries;
using PlanWriter.Domain.Dtos.WordWars;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;
using Xunit;

namespace PlanWriter.Tests.WordWar.Queries;

public class GetWordWarScoreboardQueryHandlerTests
{
    private readonly Mock<ILogger<GetWordWarScoreboardQueryHandler>> _loggerMock = new();
    private readonly Mock<IWordWarParticipantReadRepository> _wordWarParticipantReadRepositoryMock = new();
    private readonly Mock<IWordWarReadRepository> _wordWarReadRepositoryMock = new();
    private readonly Mock<IWordWarRepository> _wordWarRepositoryMock = new();

    [Fact]
    public async Task Handle_ShouldReturnScoreboardWithMetadata_AndParticipantsFromReadModel()
    {
        var query = new GetWordWarScoreboardQuery(Guid.NewGuid());
        var wordWar = new EventWordWarsDto
        {
            Id = query.WarId,
            EventId = Guid.NewGuid(),
            Status = WordWarStatus.Running,
            DurationInMinuts = 20,
            StartsAtUtc = DateTime.UtcNow.AddMinutes(-5),
            EndsAtUtc = DateTime.UtcNow.AddMinutes(15)
        };

        var participants = new List<EventWordWarParticipantsDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WordWarId = query.WarId,
                UserId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                WordsInRound = 120,
                FinalRank = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                WordWarId = query.WarId,
                UserId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                WordsInRound = 80,
                FinalRank = 2
            }
        };

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(query.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wordWar);

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetScoreboardAsync(query.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        var sut = CreateHandler();
        var result = await sut.Handle(query, CancellationToken.None);

        result.Id.Should().Be(query.WarId);
        result.Status.Should().Be(WordWarStatus.Running);
        result.DurationMinutes.Should().Be(20);
        result.RemainingSeconds.Should().BeGreaterThan(0);
        result.RemainingSecnds.Should().Be(result.RemainingSeconds);
        result.Participants.Should().HaveCount(2);
        result.Participants[0].FinalRank.Should().Be(1);
        result.Participants[1].FinalRank.Should().Be(2);

        _wordWarParticipantReadRepositoryMock.Verify(
            r => r.GetScoreboardAsync(query.WarId, It.IsAny<CancellationToken>()),
            Times.Once);
        _wordWarRepositoryMock.Verify(
            r => r.FinishAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAutoFinish_WhenRunningWarHasExpired()
    {
        var query = new GetWordWarScoreboardQuery(Guid.NewGuid());
        var wordWar = new EventWordWarsDto
        {
            Id = query.WarId,
            EventId = Guid.NewGuid(),
            Status = WordWarStatus.Running,
            DurationInMinuts = 15,
            StartsAtUtc = DateTime.UtcNow.AddMinutes(-20),
            EndsAtUtc = DateTime.UtcNow.AddSeconds(-1)
        };

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(query.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wordWar);

        _wordWarRepositoryMock
            .Setup(r => r.FinishAsync(query.WarId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _wordWarRepositoryMock
            .Setup(r => r.PersistFinalRankAsync(query.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetScoreboardAsync(query.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EventWordWarParticipantsDto>());

        var sut = CreateHandler();
        var result = await sut.Handle(query, CancellationToken.None);

        result.Status.Should().Be(WordWarStatus.Finished);
        result.RemainingSeconds.Should().Be(0);
        result.RemainingSecnds.Should().Be(0);

        _wordWarRepositoryMock.Verify(
            r => r.FinishAsync(query.WarId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _wordWarRepositoryMock.Verify(
            r => r.PersistFinalRankAsync(query.WarId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReloadWar_WhenAutoFinishReturnsZero()
    {
        var query = new GetWordWarScoreboardQuery(Guid.NewGuid());

        _wordWarReadRepositoryMock
            .SetupSequence(r => r.GetByIdAsync(query.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = query.WarId,
                Status = WordWarStatus.Running,
                DurationInMinuts = 10,
                EndsAtUtc = DateTime.UtcNow.AddSeconds(-1)
            })
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = query.WarId,
                Status = WordWarStatus.Finished,
                DurationInMinuts = 10,
                EndsAtUtc = DateTime.UtcNow.AddSeconds(-1)
            });

        _wordWarRepositoryMock
            .Setup(r => r.FinishAsync(query.WarId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetScoreboardAsync(query.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EventWordWarParticipantsDto>());

        var sut = CreateHandler();
        var result = await sut.Handle(query, CancellationToken.None);

        result.Status.Should().Be(WordWarStatus.Finished);
        _wordWarRepositoryMock.Verify(
            r => r.PersistFinalRankAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenWarDoesNotExist()
    {
        var query = new GetWordWarScoreboardQuery(Guid.NewGuid());

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(query.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarsDto?)null);

        var sut = CreateHandler();
        var act = async () => await sut.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Word war not found.");
    }

    private GetWordWarScoreboardQueryHandler CreateHandler()
    {
        return new GetWordWarScoreboardQueryHandler(
            _loggerMock.Object,
            _wordWarParticipantReadRepositoryMock.Object,
            _wordWarReadRepositoryMock.Object,
            _wordWarRepositoryMock.Object);
    }
}
