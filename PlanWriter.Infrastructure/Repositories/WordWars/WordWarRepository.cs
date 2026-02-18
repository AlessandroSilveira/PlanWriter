using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.WordWars;

public class WordWarRepository(IDbExecutor db) : IWordWarRepository
{
    private async Task<bool> IsStatusNumericAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT TOP 1
                CASE WHEN t.name IN (N'tinyint', N'smallint', N'int', N'bigint') THEN 1 ELSE 0 END
            FROM sys.columns c
            INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
            WHERE c.object_id = OBJECT_ID(N'dbo.EventWordWars')
              AND c.name = N'Status';";

        var result = await db.QueryFirstOrDefaultAsync<int?>(sql, ct: ct);
        return result == 1;
    }

    public async Task<Guid> CreateAsync(Guid eventId, Guid createdByUserId, int durationMinutes, DateTime startsAtUtc, DateTime endsAtUtc, WordWarStatus status, CancellationToken ct = default)
    {
        var statusIsNumeric = await IsStatusNumericAsync(ct);

        const string sql = @"
            INSERT INTO EventWordWars
            (
                Id,
                EventId,
                CreatedByUserId,
                Status,
                DurationInMinutes,
                StartAtUtc,
                EndAtUtc,
                CreatedAtUtc,
                FinishedAtUtc
            )
            VALUES
            (
                @Id,
                @EventId,
                @CreatedByUserId,
                @Status,
                @DurationInMinutes,
                @StartAtUtc,
                @EndAtUtc,
                SYSUTCDATETIME(),
                NULL
            );";

        var id = Guid.NewGuid();
        await db.ExecuteAsync(sql, new
        {
            Id = id,
            EventId = eventId,
            CreatedByUserId = createdByUserId,
            Status = statusIsNumeric ? (object)(int)status : status.ToString(),
            DurationInMinutes = durationMinutes,
            StartAtUtc = startsAtUtc,
            EndAtUtc = endsAtUtc
        }, ct);

        return id;
    }

    public Task<int> StartAsync(Guid warId, DateTime startsAtUtc, DateTime endsAtUtc, CancellationToken ct = default)
    {
        return StartInternalAsync(warId, startsAtUtc, endsAtUtc, ct);
    }

    private async Task<int> StartInternalAsync(Guid warId, DateTime startsAtUtc, DateTime endsAtUtc, CancellationToken ct)
    {
        var statusIsNumeric = await IsStatusNumericAsync(ct);
        const string sql = @"
            UPDATE EventWordWars
            SET Status = @StatusRunning,
                StartAtUtc = @StartAtUtc,
                EndAtUtc = @EndAtUtc
            WHERE Id = @WarId
              AND Status = @StatusWaiting;";

        return await db.ExecuteAsync(sql, new
        {
            WarId = warId,
            StartAtUtc = startsAtUtc,
            EndAtUtc = endsAtUtc,
            StatusRunning = statusIsNumeric ? (object)(int)WordWarStatus.Running : WordWarStatus.Running.ToString(),
            StatusWaiting = statusIsNumeric ? (object)(int)WordWarStatus.Waiting : WordWarStatus.Waiting.ToString(),
        }, ct);
    }

    public Task<int> FinishAsync(Guid warId, DateTime finishedAtUtc, CancellationToken ct = default)
    {
        return FinishInternalAsync(warId, finishedAtUtc, ct);
    }

    private async Task<int> FinishInternalAsync(Guid warId, DateTime finishedAtUtc, CancellationToken ct)
    {
        var statusIsNumeric = await IsStatusNumericAsync(ct);
        const string sql = @"
            UPDATE EventWordWars
            SET Status = @StatusFinished,
                FinishedAtUtc = @FinishedAtUtc
            WHERE Id = @WarId
              AND Status = @StatusRunning;";

        return await db.ExecuteAsync(sql, new
        {
            WarId = warId,
            FinishedAtUtc = finishedAtUtc,
            StatusFinished = statusIsNumeric ? (object)(int)WordWarStatus.Finished : WordWarStatus.Finished.ToString(),
            StatusRunning = statusIsNumeric ? (object)(int)WordWarStatus.Running : WordWarStatus.Running.ToString(),
        }, ct);
    }

    public Task<int> JoinAsync(Guid warId, Guid userId, Guid projectId, CancellationToken ct = default)
    {
        return JoinInternalAsync(warId, userId, projectId, ct);
    }

    private async Task<int> JoinInternalAsync(Guid warId, Guid userId, Guid projectId, CancellationToken ct)
    {
        var statusIsNumeric = await IsStatusNumericAsync(ct);
        const string sql = @"
            INSERT INTO EventWordWarParticipants
            (
                Id,
                WordWarId,
                UserId,
                ProjectId,
                JoinedAtUtc,
                WordsInRound,
                LastCheckpointAtUtc,
                FinalRank
            )
            SELECT
                @Id,
                @WarId,
                @UserId,
                @ProjectId,
                SYSUTCDATETIME(),
                0,
                NULL,
                NULL
            WHERE EXISTS
            (
                SELECT 1
                FROM EventWordWars w
                WHERE w.Id = @WarId
                  AND w.Status = @StatusWaiting
            )
            AND NOT EXISTS
            (
                SELECT 1
                FROM EventWordWarParticipants p
                WHERE p.WordWarId = @WarId
                  AND p.UserId = @UserId
            );";

        return await db.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            WarId = warId,
            UserId = userId,
            ProjectId = projectId,
            StatusWaiting = statusIsNumeric ? (object)(int)WordWarStatus.Waiting : WordWarStatus.Waiting.ToString(),
        }, ct);
    }

    public Task<int> LeaveAsync(Guid warId, Guid userId, CancellationToken ct = default)
    {
        return LeaveInternalAsync(warId, userId, ct);
    }

    private async Task<int> LeaveInternalAsync(Guid warId, Guid userId, CancellationToken ct)
    {
        var statusIsNumeric = await IsStatusNumericAsync(ct);
        const string sql = @"
            DELETE p
            FROM EventWordWarParticipants p
            INNER JOIN EventWordWars w ON w.Id = p.WordWarId
            WHERE p.WordWarId = @WarId
              AND p.UserId = @UserId
              AND w.Status = @StatusWaiting;";

        return await db.ExecuteAsync(sql, new
        {
            WarId = warId,
            UserId = userId,
            StatusWaiting = statusIsNumeric ? (object)(int)WordWarStatus.Waiting : WordWarStatus.Waiting.ToString(),
        }, ct);
    }

    public Task<int> SubmitCheckpointAsync(Guid warId, Guid userId, int wordsInRound, DateTime checkpointAtUtc, CancellationToken ct = default)
    {
        return SubmitCheckpointInternalAsync(warId, userId, wordsInRound, checkpointAtUtc, ct);
    }

    private async Task<int> SubmitCheckpointInternalAsync(Guid warId, Guid userId, int wordsInRound, DateTime checkpointAtUtc, CancellationToken ct)
    {
        var statusIsNumeric = await IsStatusNumericAsync(ct);
        const string sql = @"
            UPDATE p
            SET p.WordsInRound = @WordsInRound,
                p.LastCheckpointAtUtc = @CheckpointAtUtc
            FROM EventWordWarParticipants p
            INNER JOIN EventWordWars w ON w.Id = p.WordWarId
            WHERE p.WordWarId = @WarId
              AND p.UserId = @UserId
              AND w.Status = @StatusRunning
              AND @WordsInRound > ISNULL(p.WordsInRound, 0);";

        return await db.ExecuteAsync(sql, new
        {
            WarId = warId,
            UserId = userId,
            WordsInRound = wordsInRound,
            CheckpointAtUtc = checkpointAtUtc,
            StatusRunning = statusIsNumeric ? (object)(int)WordWarStatus.Running : WordWarStatus.Running.ToString(),
        }, ct);
    }
    
    // /Users/alessandrosilveira/Documents/Repos/PlanWriter/PlanWriter.Infrastructure/Repositories/WordWars/WordWarRepository.cs
    public Task<int> PersistFinalRankAsync(Guid warId, CancellationToken ct = default)
    {
        const string sql = @"
        ;WITH Ranked AS
        (
            SELECT
                p.Id,
                ROW_NUMBER() OVER
                (
                    ORDER BY
                        p.WordsInRound DESC,
                        p.LastCheckpointAtUtc ASC,
                        p.JoinedAtUtc ASC
                ) AS RankPos
            FROM EventWordWarParticipants p
            WHERE p.WordWarId = @WarId
        )
        UPDATE p
        SET p.FinalRank = r.RankPos
        FROM EventWordWarParticipants p
        INNER JOIN Ranked r ON r.Id = p.Id;";

        return db.ExecuteAsync(sql, new { WarId = warId }, ct);
    }

}
