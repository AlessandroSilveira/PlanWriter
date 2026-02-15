using FluentAssertions;
using PlanWriter.Domain.Dtos.WordWars;
using PlanWriter.Infrastructure.ReadModels.WordWars;
using PlanWriter.Tests.Infrastructure;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.WordWars;

public class WordWarParticipantReadRepositoryTests
{
    [Fact]
    public async Task GetScoreboardAsync_ShouldReturnParticipants_ForWar()
    {
        var warId = Guid.NewGuid();
        var rows = new[]
        {
            new EventWordWarParticipantsDto
            {
                Id = Guid.NewGuid(),
                WordWarId = warId,
                UserId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                JoinedAtUtc = DateTime.UtcNow,
                WordsInRound = 2000,
                LastCheckpointAtUtc = DateTime.UtcNow,
                FinalRank = 1
            }
        };

        string? capturedSql = null;
        object? capturedParam = null;

        var db = new StubDbExecutor
        {
            QueryAsyncHandler = (type, sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return type == typeof(EventWordWarParticipantsDto) ? rows : Array.Empty<EventWordWarParticipantsDto>();
            }
        };

        var sut = new WordWarParticipantReadRepository(db);
        var result = await sut.GetScoreboardAsync(warId, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].FinalRank.Should().Be(1);
        capturedSql.Should().Contain("ROW_NUMBER() OVER");
        capturedSql.Should().Contain("AS FinalRank");
        capturedSql.Should().Contain("WHERE p.WordWarId = @WarId");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("WarId").Should().Be(warId);
    }
}
