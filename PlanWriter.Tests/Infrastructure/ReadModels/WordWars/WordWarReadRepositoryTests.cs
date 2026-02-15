using FluentAssertions;
using PlanWriter.Domain.Dtos.WordWars;
using PlanWriter.Domain.Enums;
using PlanWriter.Infrastructure.ReadModels.WordWars;
using PlanWriter.Tests.Infrastructure;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.WordWars;

public class WordWarReadRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnWar_WhenFound()
    {
        var expected = new EventWordWarsDto
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            Status = WordWarStatus.Waiting,
            DurationInMinuts = 15,
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddMinutes(15),
            CreatedAtUtc = DateTime.UtcNow,
            FinishedAtUtc = DateTime.UtcNow
        };

        string? capturedSql = null;
        object? capturedParam = null;

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (type, sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return type == typeof(EventWordWarsDto) ? expected : null;
            }
        };

        var sut = new WordWarReadRepository(db);
        var result = await sut.GetByIdAsync(expected.Id, CancellationToken.None);

        result.Should().Be(expected);
        capturedSql.Should().Contain("WHERE Id = @WarId");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("WarId").Should().Be(expected.Id);
    }

    [Fact]
    public async Task GetActiveByEventIdAsync_ShouldReturnWar_WhenWaitingOrRunning()
    {
        var expected = new EventWordWarsDto
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            Status = WordWarStatus.Running,
            DurationInMinuts = 20,
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddMinutes(20),
            CreatedAtUtc = DateTime.UtcNow,
            FinishedAtUtc = DateTime.UtcNow
        };

        string? capturedSql = null;
        object? capturedParam = null;

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (type, sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return type == typeof(EventWordWarsDto) ? expected : null;
            }
        };

        var sut = new WordWarReadRepository(db);
        var result = await sut.GetActiveByEventIdAsync(expected.EventId, CancellationToken.None);

        result.Should().Be(expected);
        capturedSql.Should().Contain("Status IN ('Waiting', 'Running')");
        capturedSql.Should().Contain("WHERE EventId = @EventId");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("EventId").Should().Be(expected.EventId);
    }
}
