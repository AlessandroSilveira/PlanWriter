using FluentAssertions;
using PlanWriter.Domain.Enums;
using PlanWriter.Infrastructure.Repositories.WordWars;
using PlanWriter.Tests.Infrastructure;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories.WordWars;

public class WordWarRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldSendExpectedParams_AndReturnAffectedRows()
    {
        object? capturedParam = null;
        string? capturedSql = null;
        CancellationToken capturedCt = default;

        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (sql, param, ct) =>
            {
                capturedSql = sql;
                capturedParam = param;
                capturedCt = ct;
                return Task.FromResult(1);
            }
        };

        var sut = new WordWarRepository(db);
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var startsAt = DateTime.UtcNow;
        var endsAt = startsAt.AddMinutes(15);
        using var cts = new CancellationTokenSource();

        var affected = await sut.CreateAsync(eventId, userId, 15, startsAt, endsAt, WordWarStatus.Waiting, cts.Token);

        affected.Should().Be(1);
        capturedSql.Should().Contain("INSERT INTO EventWordWars");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("EventId").Should().Be(eventId);
        capturedParam.GetProp<Guid>("CreatedByUserId").Should().Be(userId);
        capturedParam.GetProp<int>("DurationInMinutes").Should().Be(15);
        capturedParam.GetProp<DateTime>("StartsAtUtc").Should().Be(startsAt);
        capturedParam.GetProp<DateTime>("EndsAtUtc").Should().Be(endsAt);
        capturedParam.GetProp<string>("Status").Should().Be(WordWarStatus.Waiting.ToString());
        capturedCt.Should().Be(cts.Token);
    }

    [Fact]
    public async Task StartAsync_ShouldUseWaitingGuard()
    {
        object? capturedParam = null;
        string? capturedSql = null;

        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return Task.FromResult(1);
            }
        };

        var sut = new WordWarRepository(db);
        var warId = Guid.NewGuid();
        var startsAt = DateTime.UtcNow;
        var endsAt = startsAt.AddMinutes(10);

        var affected = await sut.StartAsync(warId, startsAt, endsAt, CancellationToken.None);

        affected.Should().Be(1);
        capturedSql.Should().Contain("Status = 'Running'");
        capturedSql.Should().Contain("AND Status = 'Waiting'");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("WarId").Should().Be(warId);
        capturedParam.GetProp<DateTime>("StartsAtUtc").Should().Be(startsAt);
        capturedParam.GetProp<DateTime>("EndsAtUtc").Should().Be(endsAt);
    }

    [Fact]
    public async Task FinishAsync_ShouldUseRunningGuard()
    {
        object? capturedParam = null;
        string? capturedSql = null;

        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return Task.FromResult(1);
            }
        };

        var sut = new WordWarRepository(db);
        var warId = Guid.NewGuid();
        var finishedAt = DateTime.UtcNow;

        var affected = await sut.FinishAsync(warId, finishedAt, CancellationToken.None);

        affected.Should().Be(1);
        capturedSql.Should().Contain("Status = 'Finished'");
        capturedSql.Should().Contain("AND Status = 'Running'");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("WarId").Should().Be(warId);
        capturedParam.GetProp<DateTime>("FinishedAtUtc").Should().Be(finishedAt);
    }

    [Fact]
    public async Task JoinAsync_ShouldSendWarUserAndProject()
    {
        object? capturedParam = null;
        string? capturedSql = null;

        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return Task.FromResult(1);
            }
        };

        var sut = new WordWarRepository(db);
        var warId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var affected = await sut.JoinAsync(warId, userId, projectId, CancellationToken.None);

        affected.Should().Be(1);
        capturedSql.Should().Contain("INSERT INTO EventWordWarParticipants");
        capturedSql.Should().Contain("w.Status = 'Waiting'");
        capturedSql.Should().Contain("NOT EXISTS");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("WarId").Should().Be(warId);
        capturedParam.GetProp<Guid>("UserId").Should().Be(userId);
        capturedParam.GetProp<Guid>("ProjectId").Should().Be(projectId);
    }

    [Fact]
    public async Task LeaveAsync_ShouldDeleteOnlyWhileWaiting()
    {
        object? capturedParam = null;
        string? capturedSql = null;

        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return Task.FromResult(1);
            }
        };

        var sut = new WordWarRepository(db);
        var warId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var affected = await sut.LeaveAsync(warId, userId, CancellationToken.None);

        affected.Should().Be(1);
        capturedSql.Should().Contain("DELETE p");
        capturedSql.Should().Contain("AND w.Status = 'Waiting'");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("WarId").Should().Be(warId);
        capturedParam.GetProp<Guid>("UserId").Should().Be(userId);
    }

    [Fact]
    public async Task SubmitCheckpointAsync_ShouldUpdateOnlyWhenRunning_AndWordsIncrease()
    {
        object? capturedParam = null;
        string? capturedSql = null;

        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return Task.FromResult(1);
            }
        };

        var sut = new WordWarRepository(db);
        var warId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var checkpointAt = DateTime.UtcNow;

        var affected = await sut.SubmitCheckpointAsync(warId, userId, 1234, checkpointAt, CancellationToken.None);

        affected.Should().Be(1);
        capturedSql.Should().Contain("w.Status = 'Running'");
        capturedSql.Should().Contain("@WordsInRound > ISNULL(p.WordsInRound, 0)");
        capturedParam.Should().NotBeNull();
        capturedParam!.GetProp<Guid>("WarId").Should().Be(warId);
        capturedParam.GetProp<Guid>("UserId").Should().Be(userId);
        capturedParam.GetProp<int>("WordsInRound").Should().Be(1234);
        capturedParam.GetProp<DateTime>("CheckpointAtUtc").Should().Be(checkpointAt);
    }
}
