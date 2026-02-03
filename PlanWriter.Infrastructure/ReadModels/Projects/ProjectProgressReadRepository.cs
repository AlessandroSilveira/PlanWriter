using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;

namespace PlanWriter.Infrastructure.ReadModels.Projects;

public class ProjectProgressReadRepository(IDbExecutor db, AppDbContext context) : Repository<ProjectProgress>(context), IProjectProgressReadRepository
{
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
                pp.Id DESC;
        """;

        var value = await db.QueryFirstOrDefaultAsync<int?>(sql, new { ProjectId = projectId, UserId = userId, Date = date }, ct);

        return value ?? 0;
    }
    
    public async Task<List<ProjectProgress>> GetProgressHistoryAsync(Guid projectId, Guid userId)
        {
            return await DbSet.Include(pp => pp.Project)
                .Where(pp => pp.ProjectId == projectId && pp.Project.UserId == userId)
                .OrderBy(pp => pp.Date)
                .ToListAsync();
        }
        public async Task<ProjectProgress> GetByIdAsync(Guid id, Guid userId)
        {
            return await DbSet
                .Include(p => p.Project)
                .FirstOrDefaultAsync(p => p.Id == id && p.Project.UserId == userId);
        }
        
        public async Task<ProjectProgress> GetLastProgressBeforeAsync(Guid projectId, DateTime date)
        {
            return await DbSet
                .Where(p => p.ProjectId == projectId && p.Date < date)
                .OrderByDescending(p => p.Date)
                .FirstOrDefaultAsync();
        }
        
        public async Task<int> GetAccumulatedAsync(Guid projectId, GoalUnit unit, CancellationToken ct)
        {
            var q = DbSet.Where(x => x.ProjectId == projectId);
            return unit switch
            {
                GoalUnit.Words   => await q.SumAsync(x => (int?)x.WordsWritten, ct) ?? 0,
                GoalUnit.Minutes => await q.SumAsync(x => (int?)x.Minutes, ct) ?? 0,
                GoalUnit.Pages   => await q.SumAsync(x => (int?)x.Pages, ct) ?? 0,
                _ => 0
            };
        }
        
        public async Task<Dictionary<Guid, int>> GetTotalWordsByProjectIdsAsync(IEnumerable<Guid> projectIds)
        {
            return await DbSet
                .Where(p => projectIds.Contains(p.ProjectId))
                .GroupBy(p => p.ProjectId)
                .Select(g => new
                {
                    ProjectId = g.Key,
                    Total = g.Sum(x => x.WordsWritten)
                })
                .ToDictionaryAsync(x => x.ProjectId, x => x.Total);
        }
        
        public async Task<EventProjectProgressDto?> GetEventProjectProgressAsync(Guid eventId, Guid projectId)
        {
            // 1️⃣ Carrega o vínculo Projeto ↔ Evento
            var link = await Context.ProjectEvents
                .Include(pe => pe.Event)
                .FirstOrDefaultAsync(pe =>
                    pe.EventId == eventId &&
                    pe.ProjectId == projectId
                );

            if (link == null)
                return null;

            var ev = link.Event;

            var start = ev.StartsAtUtc.Date;
            var endExclusive = ev.EndsAtUtc.Date.AddDays(1);

            // 2️⃣ Soma progresso do projeto dentro da janela do evento
            var totalWritten = await DbSet
                .Where(p =>
                    p.ProjectId == projectId &&
                    p.CreatedAt >= start &&
                    p.CreatedAt < endExclusive
                )
                .SumAsync(p => (int?)p.WordsWritten) ?? 0;

            var target = link.TargetWords
                         ?? ev.DefaultTargetWords
                         ?? 0;

            var percent = target > 0
                ? Math.Round((double)totalWritten / target * 100, 2)
                : 0;

            // 3️⃣ Ranking (calculado de forma segura)
            var ranking = await Context.ProjectEvents
                .Where(pe => pe.EventId == eventId)
                .Select(pe => new
                {
                    pe.ProjectId,
                    Words = Context.ProjectProgresses
                        .Where(p =>
                            p.ProjectId == pe.ProjectId &&
                            p.CreatedAt >= start &&
                            p.CreatedAt < endExclusive
                        )
                        .Sum(p => (int?)p.WordsWritten) ?? 0
                })
                .OrderByDescending(x => x.Words)
                .Select((x, index) => new { x.ProjectId, Rank = index + 1 })
                .FirstOrDefaultAsync(x => x.ProjectId == projectId);

            return new EventProjectProgressDto
            {
                EventId = eventId,
                ProjectId = projectId,
                TotalWrittenInEvent = totalWritten,
                TargetWords = target,
                Percent = percent,
                Rank = ranking?.Rank
            };
        }
        public async Task<Dictionary<Guid, int>> GetTotalWordsByUsersAsync(IEnumerable<Guid> userIds, DateTime? start, DateTime? end)
        {
            var query = Context.ProjectProgresses
                .Include(p => p.Project)
                .Where(p => userIds.Contains<Guid>(p.Project.UserId));

            if (start.HasValue && start.Value > DateTime.MinValue)
            {
                query = query.Where(p => p.Date >= start.Value);
            }

            if (end.HasValue)
            {
                query = query.Where(p => p.Date <= end.Value);
            }

            var result = await query
                .GroupBy(p => p.Project.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Total = g.Sum(x => x.WordsWritten)
                })
                .ToDictionaryAsync(x => x.UserId, x => x.Total);

            return result;
        }
        public async Task<int> GetMonthlyWordsAsync(Guid userId, DateTime start, DateTime end)
        {
            return await DbSet
                .Include(p => p.Project)
                .Where(p =>
                    p.Project.UserId == userId &&
                    p.Date >= start &&
                    p.Date < end
                )
                .SumAsync(p => p.WordsWritten);
        }
        
        public async Task<IEnumerable<ProjectProgress>> GetProgressByProjectIdAsync(Guid projectId, Guid userId)
        {
            return await DbSet
                .Where(p => p.ProjectId == projectId && p.Project.UserId == userId)
                .OrderBy(p => p.Date)
                .ToListAsync();
        }
}
