using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.ProjectEvents;

public sealed class ProjectEventsReadRepository(IDbExecutor db) : IProjectEventsReadRepository
{
    /* ===================== PRIVATE ROWS ===================== */

  
    /* ===================== METHODS ===================== */

    public async Task<ProjectEvent?> GetByProjectAndEventWithEventAsync(Guid projectId, Guid eventId, CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP 1
                pe.Id,
                pe.ProjectId,
                pe.EventId,
                pe.TargetWords,
                pe.Won,
                pe.ValidatedAtUtc,
                pe.FinalWordCount,
                pe.ValidatedWords,
                pe.ValidationSource,

                e.Name              AS EventName,
                e.Slug              AS EventSlug,
                e.[Type]            AS EventType,
                e.StartsAtUtc,
                e.EndsAtUtc,
                e.DefaultTargetWords,
                e.IsActive
            FROM ProjectEvents pe
            INNER JOIN Events e ON e.Id = pe.EventId
            WHERE pe.ProjectId = @ProjectId
              AND pe.EventId   = @EventId;
        ";

        var row = await db.QueryFirstOrDefaultAsync<ProjectEventWithEventRow>(
            sql,
            new { ProjectId = projectId, EventId = eventId },
            ct: ct
        );

        return row is null ? null : Map(row);
    }

    public async Task<ProjectEvent?> GetMostRecentWinByUserIdAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP 1
                pe.Id,
                pe.ProjectId,
                pe.EventId,
                pe.TargetWords,
                pe.Won,
                pe.ValidatedAtUtc,
                pe.FinalWordCount,
                pe.ValidatedWords,
                pe.ValidationSource,

                e.Name              AS EventName,
                e.Slug              AS EventSlug,
                e.[Type]            AS EventType,
                e.StartsAtUtc,
                e.EndsAtUtc,
                e.DefaultTargetWords,
                e.IsActive
            FROM ProjectEvents pe
            INNER JOIN Projects p ON p.Id = pe.ProjectId
            INNER JOIN Events e   ON e.Id = pe.EventId
            WHERE p.UserId = @UserId
              AND pe.Won = 1
              AND pe.ValidatedAtUtc IS NOT NULL
            ORDER BY pe.ValidatedAtUtc DESC;
        ";

        var row = await db.QueryFirstOrDefaultAsync<ProjectEventWithEventRow>(
            sql,
            new { UserId = userId },
            ct: ct
        );

        return row is null ? null : Map(row);
    }

    public async Task<ProjectEvent?> GetByIdWithEventAsync(Guid projectEventId, CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP 1
                pe.Id,
                pe.ProjectId,
                pe.EventId,
                pe.TargetWords,
                pe.Won,
                pe.ValidatedAtUtc,
                pe.FinalWordCount,
                pe.ValidatedWords,
                pe.ValidationSource,

                e.Name              AS EventName,
                e.Slug              AS EventSlug,
                e.[Type]            AS EventType,
                e.StartsAtUtc,
                e.EndsAtUtc,
                e.DefaultTargetWords,
                e.IsActive
            FROM ProjectEvents pe
            INNER JOIN Events e ON e.Id = pe.EventId
            WHERE pe.Id = @Id;
        ";

        var row = await db.QueryFirstOrDefaultAsync<ProjectEventWithEventRow>(
            sql,
            new { Id = projectEventId },
            ct: ct
        );

        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<ProjectEvent>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                pe.Id,
                pe.ProjectId,
                pe.EventId,
                pe.TargetWords,
                pe.Won,
                pe.ValidatedAtUtc,
                pe.FinalWordCount,
                pe.ValidatedWords,
                pe.ValidationSource,

                e.Name              AS EventName,
                e.Slug              AS EventSlug,
                e.[Type]            AS EventType,
                e.StartsAtUtc,
                e.EndsAtUtc,
                e.DefaultTargetWords,
                e.IsActive
            FROM ProjectEvents pe
            INNER JOIN Projects p ON p.Id = pe.ProjectId
            INNER JOIN Events e   ON e.Id = pe.EventId
            WHERE p.UserId = @UserId;
        ";

        var rows = await db.QueryAsync<ProjectEventWithEventRow>(
            sql,
            new { UserId = userId },
            ct: ct
        );

        return rows.Select(Map).ToList();
    }

    /* ===================== MAPPER ===================== */

    private static ProjectEvent Map(ProjectEventWithEventRow row)
    {
        return new ProjectEvent
        {
            Id = row.Id,
            ProjectId = row.ProjectId,
            EventId = row.EventId,
            TargetWords = row.TargetWords,
            Won = row.Won,
            ValidatedAtUtc = row.ValidatedAtUtc,
            FinalWordCount = row.FinalWordCount,
            ValidatedWords = row.ValidatedWords,
            ValidationSource = row.ValidationSource,

            Event = new Event
            {
                Id = row.EventId,
                Name = row.EventName,
                Slug = row.EventSlug,
                Type = (EventType)row.EventType,
                StartsAtUtc = row.StartsAtUtc,
                EndsAtUtc = row.EndsAtUtc,
                DefaultTargetWords = row.DefaultTargetWords,
                IsActive = row.IsActive
            }
        };
    }
}
