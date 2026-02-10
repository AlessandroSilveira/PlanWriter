using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.ProjectEvents;

public sealed class ProjectEventsRepository(IDbExecutor db) : IProjectEventsRepository
{
    public Task<ProjectEvent?> GetByProjectAndEventAsync(Guid projectId, Guid eventId, CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP 1
                Id,
                ProjectId,
                EventId,
                TargetWords,
                Won,
                ValidatedAtUtc,
                FinalWordCount
            FROM ProjectEvents
            WHERE ProjectId = @ProjectId
              AND EventId   = @EventId;
        ";

        return db.QueryFirstOrDefaultAsync<ProjectEvent>(
            sql,
            new { ProjectId = projectId, EventId = eventId },
            ct: ct
        );
    }

    public Task CreateAsync(ProjectEvent entity, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO ProjectEvents
            (
                Id,
                ProjectId,
                EventId,
                TargetWords,
                Won,
                ValidatedAtUtc,
                FinalWordCount
            )
            VALUES
            (
                @Id,
                @ProjectId,
                @EventId,
                @TargetWords,
                @Won,
                @ValidatedAtUtc,
                @FinalWordCount
            );
        ";

        // garanta Id
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        return db.ExecuteAsync(
            sql,
            new
            {
                entity.Id,
                entity.ProjectId,
                entity.EventId,
                entity.TargetWords,
                entity.Won,
                entity.ValidatedAtUtc,
                entity.FinalWordCount
            },
            ct: ct
        );
    }

    public Task UpdateTargetWordsAsync(Guid projectEventId, int targetWords, CancellationToken ct)
    {
        const string sql = @"
            UPDATE ProjectEvents
            SET TargetWords = @TargetWords
            WHERE Id = @Id;
        ";

        return db.ExecuteAsync(sql, new { Id = projectEventId, TargetWords = targetWords }, ct: ct);
    }

    public async Task<bool> RemoveByKeysAsync(Guid projectId, Guid eventId, CancellationToken ct)
    {
        const string sql = @"
            DELETE FROM ProjectEvents
            WHERE ProjectId = @ProjectId
              AND EventId   = @EventId;
        ";

        var affected = await db.ExecuteAsync(sql, new { ProjectId = projectId, EventId = eventId }, ct: ct);
        return affected > 0;
    }
    
    public Task UpdateProjectEvent(ProjectEvent entity, CancellationToken ct)
    {
        const string sql = @"
        UPDATE ProjectEvents
        SET
            TargetWords     = @TargetWords,
            Won             = @Won,
            ValidatedAtUtc  = @ValidatedAtUtc,
            FinalWordCount  = @FinalWordCount
        WHERE Id = @Id;
    ";

        return db.ExecuteAsync(
            sql,
            new
            {
                entity.Id,
                entity.TargetWords,
                entity.Won,
                entity.ValidatedAtUtc,
                entity.FinalWordCount
            },
            ct: ct
        );
    }
    
    public Task RemoveByKeys(Guid requestProjectId, Guid requestEventId)
    {
        const string sql = @"
        DELETE FROM ProjectEvents
        WHERE ProjectId = @ProjectId
          AND EventId   = @EventId;
    ";

        return db.ExecuteAsync(
            sql,
            new
            {
                ProjectId = requestProjectId,
                EventId   = requestEventId
            }
        );
    }

}