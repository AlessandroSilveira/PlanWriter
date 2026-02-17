using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class EventRepository(IDbExecutor db) : IEventRepository
{
    private sealed record EventRow(
        Guid Id,
        string Name,
        string Slug,
        int Type,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        int? DefaultTargetWords,
        bool IsActive,
        DateTime? ValidationWindowStartsAtUtc,
        DateTime? ValidationWindowEndsAtUtc,
        string? AllowedValidationSources);

    public async Task<List<EventDto>> GetActiveEvents()
    {
        var now = DateTime.UtcNow;
        const string sql = @"
            SELECT
                Id,
                Name,
                Slug,
                [Type],
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                IsActive,
                ValidationWindowStartsAtUtc,
                ValidationWindowEndsAtUtc,
                AllowedValidationSources
            FROM Events
            WHERE IsActive = 1
              AND StartsAtUtc <= @Now
              AND EndsAtUtc >= @Now;
        ";

        var rows = await db.QueryAsync<EventRow>(sql, new { Now = now });
        return rows.Select(ToDto).ToList();
    }

    public async Task<bool> GetEventBySlug(string reqSlug)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Events
            WHERE Slug = @Slug;
        ";

        var count = await db.QueryFirstOrDefaultAsync<int>(sql, new { Slug = reqSlug });
        return count > 0;
    }

    public Task AddEvent(Event ev)
    {
        const string sql = @"
            INSERT INTO Events
            (
                Id,
                Name,
                Slug,
                [Type],
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                ValidationWindowStartsAtUtc,
                ValidationWindowEndsAtUtc,
                AllowedValidationSources,
                IsActive
            )
            VALUES
            (
                @Id,
                @Name,
                @Slug,
                @Type,
                @StartsAtUtc,
                @EndsAtUtc,
                @DefaultTargetWords,
                @ValidationWindowStartsAtUtc,
                @ValidationWindowEndsAtUtc,
                @AllowedValidationSources,
                @IsActive
            );
        ";

        if (ev.Id == Guid.Empty)
            ev.Id = Guid.NewGuid();

        return db.ExecuteAsync(sql, new
        {
            ev.Id,
            ev.Name,
            ev.Slug,
            Type = (int)ev.Type,
            ev.StartsAtUtc,
            ev.EndsAtUtc,
            ev.DefaultTargetWords,
            ev.ValidationWindowStartsAtUtc,
            ev.ValidationWindowEndsAtUtc,
            ev.AllowedValidationSources,
            ev.IsActive
        });
    }

    public Task<Event?> GetEventById(Guid reqEventId)
    {
        const string sql = @"
            SELECT TOP 1
                Id,
                Name,
                Slug,
                [Type],
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                ValidationWindowStartsAtUtc,
                ValidationWindowEndsAtUtc,
                AllowedValidationSources,
                IsActive
            FROM Events
            WHERE Id = @Id;
        ";

        return db.QueryFirstOrDefaultAsync<Event>(sql, new { Id = reqEventId });
    }

    public async Task<List<EventDto>?> GetAllAsync()
    {
        const string sql = @"
            SELECT
                Id,
                Name,
                Slug,
                [Type],
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                IsActive,
                ValidationWindowStartsAtUtc,
                ValidationWindowEndsAtUtc,
                AllowedValidationSources
            FROM Events;
        ";

        var rows = await db.QueryAsync<EventRow>(sql);
        return rows.Select(ToDto).ToList();
    }

    public Task UpdateAsync(Event ev, Guid id)
    {
        const string sql = @"
            UPDATE Events
            SET
                Name = @Name,
                Slug = @Slug,
                [Type] = @Type,
                StartsAtUtc = @StartsAtUtc,
                EndsAtUtc = @EndsAtUtc,
                DefaultTargetWords = @DefaultTargetWords,
                ValidationWindowStartsAtUtc = @ValidationWindowStartsAtUtc,
                ValidationWindowEndsAtUtc = @ValidationWindowEndsAtUtc,
                AllowedValidationSources = @AllowedValidationSources,
                IsActive = @IsActive
            WHERE Id = @Id;
        ";

        return db.ExecuteAsync(sql, new
        {
            Id = id,
            ev.Name,
            ev.Slug,
            Type = (int)ev.Type,
            ev.StartsAtUtc,
            ev.EndsAtUtc,
            ev.DefaultTargetWords,
            ev.ValidationWindowStartsAtUtc,
            ev.ValidationWindowEndsAtUtc,
            ev.AllowedValidationSources,
            ev.IsActive
        });
    }
    
    public Task DeleteAsync(Event ev)
    {
        const string sql = @"
            DELETE FROM Events
            WHERE Id = @Id;
        ";

        return db.ExecuteAsync(sql, new { ev.Id });
    }

    public async Task<List<MyEventDto>> GetEventByUserId(Guid userId)
    {
        const string sql = @"
            SELECT
                pe.ProjectId,
                pe.EventId,
                e.Name AS EventName,
                p.Title AS ProjectTitle,
                COALESCE(
                    SUM(
                        CASE
                            WHEN pp.CreatedAt >= e.StartsAtUtc
                                 AND pp.CreatedAt < DATEADD(day, 1, e.EndsAtUtc)
                            THEN pp.WordsWritten
                            ELSE 0
                        END
                    ),
                    0
                ) AS TotalWrittenInEvent,
                COALESCE(pe.TargetWords, e.DefaultTargetWords, 50000) AS TargetWords
            FROM ProjectEvents pe
            INNER JOIN Events e ON e.Id = pe.EventId
            INNER JOIN Projects p ON p.Id = pe.ProjectId
            LEFT JOIN ProjectProgresses pp ON pp.ProjectId = pe.ProjectId
            WHERE p.UserId = @UserId
            GROUP BY
                pe.ProjectId,
                pe.EventId,
                e.Name,
                p.Title,
                pe.TargetWords,
                e.DefaultTargetWords;
        ";

        var rows = await db.QueryAsync<MyEventDto>(sql, new { UserId = userId });
        return rows.ToList();
    }

    public async Task<List<EventLeaderboardRowDto>> GetLeaderboard(
        Event ev,
        DateTime winStart,
        DateTime winEnd,
        int top)
    {
        const string sql = @"
            SELECT
                pe.ProjectId,
                p.Title AS ProjectTitle,
                (u.FirstName + ' ' + u.LastName) AS UserName,
                COALESCE(agg.Words, 0) AS Words,
                COALESCE(pe.TargetWords, e.DefaultTargetWords, 50000) AS TargetWords
            FROM ProjectEvents pe
            INNER JOIN Projects p ON p.Id = pe.ProjectId
            INNER JOIN Users u ON u.Id = p.UserId
            INNER JOIN Events e ON e.Id = pe.EventId
            LEFT JOIN (
                SELECT
                    ProjectId,
                    SUM(WordsWritten) AS Words
                FROM ProjectProgresses
                WHERE CreatedAt >= @StartUtc
                  AND CreatedAt <  DATEADD(day, 1, @EndUtc)
                GROUP BY ProjectId
            ) agg ON agg.ProjectId = pe.ProjectId
            WHERE pe.EventId = @EventId;
        ";

        var rows = await db.QueryAsync<EventLeaderboardRowDto>(sql, new
        {
            EventId = ev.Id,
            StartUtc = winStart,
            EndUtc = winEnd,
            Top = Math.Clamp(top, 1, 200)
        });

        return rows.ToList();
    }

    private static EventDto ToDto(EventRow row)
        => new(row.Id, row.Name, row.Slug, ((EventType)row.Type).ToString(),
            row.StartsAtUtc, row.EndsAtUtc, row.DefaultTargetWords, row.IsActive,
            row.ValidationWindowStartsAtUtc, row.ValidationWindowEndsAtUtc, row.AllowedValidationSources);
}
