// Infrastructure/Repositories/ProjectProgressRepository.cs
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Infrastructure.Repositories
{
    public class ProjectProgressRepository(AppDbContext context, IDbConnection connection)
        : Repository<ProjectProgress>(context), IProjectProgressRepository
    {
        public async Task<IEnumerable<ProjectProgress>> GetProgressByProjectIdAsync(Guid projectId, Guid userId)
        {
            return await DbSet
                .Where(p => p.ProjectId == projectId && p.Project.UserId == userId)
                .OrderBy(p => p.Date)
                .ToListAsync();
        }
        
        // public async Task<ProjectProgress> AddProgressAsync(ProjectProgress progress)
        // {
        //     progress.Id = Guid.NewGuid();
        //     if (progress.Date == default)
        //         progress.Date = DateTime.UtcNow;
        //
        //     await DbSet.AddAsync(progress);
        //     await Context.SaveChangesAsync();
        //
        //     return progress;
        // }
        
        public async Task<ProjectProgress> AddProgressAsync(ProjectProgress progress, CancellationToken ct)
        {
            const string sql = @"
                INSERT INTO ProjectProgresses
                (
                    Id,
                    ProjectId,
                    WordsWritten,
                    Minutes,
                    Pages,
                    TotalWordsWritten,
                    RemainingWords,
                    RemainingPercentage,
                    [Date],
                    Notes,
                    CreatedAt
                )
                VALUES
                (
                    @Id,
                    @ProjectId,
                    @WordsWritten,
                    @Minutes,
                    @Pages,
                    @TotalWordsWritten,
                    @RemainingWords,
                    @RemainingPercentage,
                    @Date,
                    @Notes,
                    @CreatedAt
                );";

            // Se sua tabela não tem CreatedAt, remove daqui
            var entity = progress;
            entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        entity.Id,
                        entity.ProjectId,
                        entity.WordsWritten,
                        entity.Minutes,
                        entity.Pages,
                        entity.TotalWordsWritten,
                        entity.RemainingWords,
                        entity.RemainingPercentage,
                        Date = entity.Date,   // cuidado com nome reservado
                        entity.Notes,
                        entity.CreatedAt
                    },
                    cancellationToken: ct
                )
            );
            return progress;
        }
        
        public async Task<List<ProjectProgress>> GetProgressHistoryAsync(Guid projectId, Guid userId)
        {
            return await DbSet
                .Include(pp => pp.Project)
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

        // public async Task<bool> DeleteAsync(Guid id, Guid userId)
        // {
        //     var progress = await GetByIdAsync(id, userId);
        //     if (progress == null)
        //         return false;
        //
        //     DbSet.Remove(progress);
        //     await Context.SaveChangesAsync();
        //     return true;
        // }
        
        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            const string sql = @"
                DELETE pp
                FROM ProjectProgresses pp
                WHERE pp.Id = @id
                  AND EXISTS (
                      SELECT 1
                      FROM Projects p
                      WHERE p.Id = pp.ProjectId
                        AND p.UserId = @userId
                  );
                ";

            var affected = await connection.ExecuteAsync(sql, new { id, userId });
            return affected > 0;
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

        
        public async Task<Dictionary<Guid, int>> GetTotalWordsByProjectIdsAsync(
            IEnumerable<Guid> projectIds)
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
        
    }
}