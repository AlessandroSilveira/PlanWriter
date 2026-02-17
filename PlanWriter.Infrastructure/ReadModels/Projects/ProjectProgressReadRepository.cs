using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Projects;

public class ProjectProgressReadRepository(IDbExecutor db) : IProjectProgressReadRepository
{
    private sealed record RankRow(int Words, int Rank);
    public Task<IReadOnlyList<ProgressHistoryRow>> GetProgressHistoryAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                pp.[Date]       AS [Date],
                pp.WordsWritten AS WordsWritten
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.ProjectId = @ProjectId
              AND p.UserId = @UserId
            ORDER BY pp.[Date] ASC;
        """;

        return db.QueryAsync<ProgressHistoryRow>(sql, new { ProjectId = projectId, UserId = userId }, ct);
    }

    public Task<IReadOnlyList<ProgressHistoryRow>> GetUserProgressByDayAsync(
        Guid userId,
        DateTime startDate,
        DateTime endDate,
        Guid? projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                CAST(pp.[Date] AS date) AS [Date],
                SUM(pp.WordsWritten)    AS WordsWritten
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE p.UserId = @UserId
              AND CAST(pp.[Date] AS date) >= @StartDate
              AND CAST(pp.[Date] AS date) <= @EndDate
              AND (@ProjectId IS NULL OR pp.ProjectId = @ProjectId)
            GROUP BY CAST(pp.[Date] AS date)
            ORDER BY CAST(pp.[Date] AS date) ASC;
        """;

        return db.QueryAsync<ProgressHistoryRow>(
            sql,
            new
            {
                UserId = userId,
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                ProjectId = projectId
            },
            ct);
    }

    public async Task<int> GetMonthlyWordsAsync(Guid userId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        const string sql = """
            SELECT COALESCE(SUM(pp.WordsWritten), 0)
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE p.UserId = @UserId
              AND pp.[Date] >= @StartUtc
              AND pp.[Date] <  @EndUtc;
        """;

        return await db.QueryFirstOrDefaultAsync<int>(sql, new { UserId = userId, StartUtc = startUtc, EndUtc = endUtc }, ct);
    }

    public Task<IReadOnlyList<ProjectProgressDayDto>> GetProgressByDayAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                CAST(pp.[Date] AS date) AS [Date],
                SUM(pp.WordsWritten) AS TotalWords,
                SUM(pp.Minutes) AS TotalMinutes,
                SUM(pp.Pages) AS TotalPages
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.ProjectId = @ProjectId
              AND p.UserId = @UserId
            GROUP BY CAST(pp.[Date] AS date)
            ORDER BY CAST(pp.[Date] AS date);
        """;

        return db.QueryAsync<ProjectProgressDayDto>(sql, new { ProjectId = projectId, UserId = userId }, ct);
    }

    public Task<ProgressRow?> GetByIdAsync(Guid progressId, Guid userId, CancellationToken ct)
    {
        const string sql = """
            SELECT 
                pp.Id,
                pp.ProjectId,
                pp.[Date]
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.Id = @ProgressId
              AND p.UserId = @UserId;
        """;

        return db.QueryFirstOrDefaultAsync<ProgressRow>(sql, new { ProgressId = progressId, UserId = userId }, ct);
    }

    public Task<ProgressRow?> GetLastProgressBeforeAsync(Guid projectId, Guid userId, DateTime date, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1
                pp.TotalWordsWritten
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.ProjectId = @ProjectId
              AND p.UserId = @UserId
              AND pp.[Date] < @Date
            ORDER BY
                pp.[Date] DESC,
                pp.CreatedAt DESC,
                pp.Id DESC;
        """;

        return db.QueryFirstOrDefaultAsync<ProgressRow>(sql, new { ProjectId = projectId, UserId = userId, Date = date }, ct);
    }

    public async Task<int> GetLastTotalBeforeAsync(Guid projectId, Guid userId, DateTime date, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1
                pp.TotalWordsWritten
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.ProjectId = @ProjectId
              AND p.UserId = @UserId
              AND pp.[Date] < @Date
            ORDER BY
                pp.[Date] DESC,
                pp.CreatedAt DESC,
                pp.Id DESC;
        """;

        var value = await db.QueryFirstOrDefaultAsync<int?>(sql, new { ProjectId = projectId, UserId = userId, Date = date }, ct);

        return value ?? 0;
    }

    public async Task<List<ProjectProgress>> GetProgressHistoryAsync(Guid projectId, Guid userId)
    {
        const string sql = @"
            SELECT
                pp.Id,
                pp.ProjectId,
                pp.TotalWordsWritten,
                pp.RemainingWords,
                pp.RemainingPercentage,
                pp.CreatedAt,
                pp.[Date],
                pp.TimeSpentInMinutes,
                pp.WordsWritten,
                pp.Notes,
                pp.Minutes,
                pp.Pages
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.ProjectId = @ProjectId
              AND p.UserId = @UserId
            ORDER BY pp.[Date] ASC;
        ";

        var rows = await db.QueryAsync<ProjectProgress>(sql, new { ProjectId = projectId, UserId = userId });
        return rows.ToList();
    }

    public Task<ProjectProgress?> GetByIdAsync(Guid id, Guid userId)
    {
        const string sql = @"
            SELECT TOP 1
                pp.Id,
                pp.ProjectId,
                pp.TotalWordsWritten,
                pp.RemainingWords,
                pp.RemainingPercentage,
                pp.CreatedAt,
                pp.[Date],
                pp.TimeSpentInMinutes,
                pp.WordsWritten,
                pp.Notes,
                pp.Minutes,
                pp.Pages
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.Id = @Id
              AND p.UserId = @UserId;
        ";

        return db.QueryFirstOrDefaultAsync<ProjectProgress>(sql, new { Id = id, UserId = userId });
    }

    public Task<ProjectProgress?> GetLastProgressBeforeAsync(Guid projectId, DateTime date)
    {
        const string sql = @"
            SELECT TOP 1
                pp.Id,
                pp.ProjectId,
                pp.TotalWordsWritten,
                pp.RemainingWords,
                pp.RemainingPercentage,
                pp.CreatedAt,
                pp.[Date],
                pp.TimeSpentInMinutes,
                pp.WordsWritten,
                pp.Notes,
                pp.Minutes,
                pp.Pages
            FROM ProjectProgresses pp
            WHERE pp.ProjectId = @ProjectId
              AND pp.[Date] < @Date
            ORDER BY pp.[Date] DESC;
        ";

        return db.QueryFirstOrDefaultAsync<ProjectProgress>(sql, new { ProjectId = projectId, Date = date });
    }

    public async Task<int> GetAccumulatedAsync(Guid projectId, GoalUnit unit, CancellationToken ct)
    {
        var column = unit switch
        {
            GoalUnit.Minutes => "Minutes",
            GoalUnit.Pages => "Pages",
            _ => "WordsWritten"
        };

        var sql = $@"
            SELECT COALESCE(SUM({column}), 0)
            FROM ProjectProgresses
            WHERE ProjectId = @ProjectId;
        ";

        return await db.QueryFirstOrDefaultAsync<int>(sql, new { ProjectId = projectId }, ct);
    }

    public async Task<Dictionary<Guid, int>> GetTotalWordsByProjectIdsAsync(IEnumerable<Guid> projectIds)
    {
        var ids = projectIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, int>();

        const string sql = @"
            SELECT
                ProjectId,
                SUM(WordsWritten) AS Total
            FROM ProjectProgresses
            WHERE ProjectId IN @ProjectIds
            GROUP BY ProjectId;
        ";

        var rows = await db.QueryAsync<(Guid ProjectId, int Total)>(sql, new { ProjectIds = ids });
        return rows.ToDictionary(x => x.ProjectId, x => x.Total);
    }

    public async Task<EventProjectProgressDto?> GetEventProjectProgressAsync(Guid eventId, Guid projectId)
    {
        const string linkSql = @"
            SELECT TOP 1
                pe.TargetWords,
                e.DefaultTargetWords,
                e.StartsAtUtc,
                e.EndsAtUtc
            FROM ProjectEvents pe
            INNER JOIN Events e ON e.Id = pe.EventId
            WHERE pe.EventId = @EventId
              AND pe.ProjectId = @ProjectId;
        ";

        var link = await db.QueryFirstOrDefaultAsync<(int? TargetWords, int? DefaultTargetWords, DateTime StartsAtUtc, DateTime EndsAtUtc)>(
            linkSql,
            new { EventId = eventId, ProjectId = projectId }
        );

        if (link == default)
            return null;

        var start = link.StartsAtUtc.Date;
        var endExclusive = link.EndsAtUtc.Date.AddDays(1);

        const string rankSql = @"
            WITH Agg AS (
                SELECT
                    pe.ProjectId,
                    SUM(CASE
                        WHEN pp.CreatedAt >= @StartUtc AND pp.CreatedAt < @EndUtc THEN pp.WordsWritten
                        ELSE 0
                    END) AS Words
                FROM ProjectEvents pe
                LEFT JOIN ProjectProgresses pp ON pp.ProjectId = pe.ProjectId
                WHERE pe.EventId = @EventId
                GROUP BY pe.ProjectId
            ),
            Ranked AS (
                SELECT
                    ProjectId,
                    Words,
                    ROW_NUMBER() OVER (ORDER BY Words DESC) AS Rank
                FROM Agg
            )
            SELECT TOP 1
                Words,
                Rank
            FROM Ranked
            WHERE ProjectId = @ProjectId;
        ";

        var rankRow = await db.QueryFirstOrDefaultAsync<RankRow>(
            rankSql,
            new { EventId = eventId, ProjectId = projectId, StartUtc = start, EndUtc = endExclusive }
        );

        var totalWritten = rankRow?.Words ?? 0;
        var target = link.TargetWords ?? link.DefaultTargetWords ?? 0;

        var percent = target > 0
            ? Math.Round((double)totalWritten / target * 100, 2)
            : 0;

        return new EventProjectProgressDto
        {
            EventId = eventId,
            ProjectId = projectId,
            TotalWrittenInEvent = totalWritten,
            TargetWords = target,
            Percent = percent,
            Rank = rankRow?.Rank
        };
    }

    public async Task<Dictionary<Guid, int>> GetTotalWordsByUsersAsync(IEnumerable<Guid> userIds, DateTime? start, DateTime? end)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, int>();

        const string sql = @"
            SELECT
                p.UserId,
                SUM(pp.WordsWritten) AS Total
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE p.UserId IN @UserIds
              AND (@Start IS NULL OR pp.[Date] >= @Start)
              AND (@End IS NULL OR pp.[Date] <= @End)
            GROUP BY p.UserId;
        ";

        var rows = await db.QueryAsync<(Guid UserId, int Total)>(
            sql,
            new { UserIds = ids, Start = start, End = end }
        );

        return rows.ToDictionary(x => x.UserId, x => x.Total);
    }

    public async Task<int> GetMonthlyWordsAsync(Guid userId, DateTime start, DateTime end)
    {
        const string sql = @"
            SELECT COALESCE(SUM(pp.WordsWritten), 0)
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE p.UserId = @UserId
              AND pp.[Date] >= @Start
              AND pp.[Date] <  @End;
        ";

        return await db.QueryFirstOrDefaultAsync<int>(sql, new { UserId = userId, Start = start, End = end });
    }
    
    public Task<IReadOnlyList<ProjectProgress>> GetProgressByProjectIdAsync(
        Guid projectId,
        Guid userId,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT
                pp.Id,
                pp.ProjectId,
                pp.TotalWordsWritten,
                pp.RemainingWords,
                pp.RemainingPercentage,
                pp.CreatedAt,
                pp.[Date],
                pp.TimeSpentInMinutes,
                pp.WordsWritten,
                pp.Notes,
                pp.Minutes,
                pp.Pages
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.ProjectId = @ProjectId
              AND p.UserId     = @UserId
            ORDER BY pp.[Date] ASC;
        ";

        return db.QueryAsync<ProjectProgress>(sql,
            new
            {
                ProjectId = projectId,
                UserId = userId
            },
            ct: ct
        );
    }
}
