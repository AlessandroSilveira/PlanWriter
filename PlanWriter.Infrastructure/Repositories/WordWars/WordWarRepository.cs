using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.WordWars;

public class WordWarRepository(IDbExecutor db) : IWordWarRepository
{
    public async Task<Guid> CreateAsync(Guid eventId, Guid createdByUserId, int durationMinutes, DateTime startsAtUtc, DateTime endsAtUtc, WordWarStatus status, CancellationToken ct = default)
    {
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
            Status = status.ToString(), // se coluna for INT, use (int)status
            DurationInMinutes = durationMinutes,
            StartAtUtc = startsAtUtc,
            EndAtUtc = endsAtUtc
        }, ct);

        return id;
    }

    public Task<int> StartAsync(Guid warId, DateTime startsAtUtc, DateTime endsAtUtc, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE EventWordWars
            SET Status = 'Running',
                StartAtUtc = @StartAtUtc,
                EndAtUtc = @EndAtUtc
            WHERE Id = @WarId
              AND Status = 'Waiting';";

        return db.ExecuteAsync(sql, new { WarId = warId, StartAtUtc = startsAtUtc, EndAtUtc = endsAtUtc }, ct);
    }

    public Task<int> FinishAsync(Guid warId, DateTime finishedAtUtc, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE EventWordWars
            SET Status = 'Finished',
                FinishedAtUtc = @FinishedAtUtc
            WHERE Id = @WarId
              AND Status = 'Running';";

        return db.ExecuteAsync(sql, new { WarId = warId, FinishedAtUtc = finishedAtUtc }, ct);
    }

    public Task<int> JoinAsync(Guid warId, Guid userId, Guid projectId, CancellationToken ct = default)
    {
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
                  AND w.Status = 'Waiting'
            )
            AND NOT EXISTS
            (
                SELECT 1
                FROM EventWordWarParticipants p
                WHERE p.WordWarId = @WarId
                  AND p.UserId = @UserId
            );";

        return db.ExecuteAsync(sql, new { Id = Guid.NewGuid(), WarId = warId, UserId = userId, ProjectId = projectId }, ct);
    }

    public Task<int> LeaveAsync(Guid warId, Guid userId, CancellationToken ct = default)
    {
        const string sql = @"
            DELETE p
            FROM EventWordWarParticipants p
            INNER JOIN EventWordWars w ON w.Id = p.WordWarId
            WHERE p.WordWarId = @WarId
              AND p.UserId = @UserId
              AND w.Status = 'Waiting';";

        return db.ExecuteAsync(sql, new { WarId = warId, UserId = userId }, ct);
    }

    public Task<int> SubmitCheckpointAsync(Guid warId, Guid userId, int wordsInRound, DateTime checkpointAtUtc, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE p
            SET p.WordsInRound = @WordsInRound,
                p.LastCheckpointAtUtc = @CheckpointAtUtc
            FROM EventWordWarParticipants p
            INNER JOIN EventWordWars w ON w.Id = p.WordWarId
            WHERE p.WordWarId = @WarId
              AND p.UserId = @UserId
              AND w.Status = 'Running'
              AND @WordsInRound > ISNULL(p.WordsInRound, 0);";

        return db.ExecuteAsync(sql, new
        {
            WarId = warId,
            UserId = userId,
            WordsInRound = wordsInRound,
            CheckpointAtUtc = checkpointAtUtc
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
