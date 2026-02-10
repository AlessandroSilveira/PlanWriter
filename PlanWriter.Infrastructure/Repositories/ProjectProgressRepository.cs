using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories
{
    public class ProjectProgressRepository(IDbExecutor db)
        : IProjectProgressRepository
    {
        
        public async Task<ProjectProgress> AddProgressAsync(ProjectProgress entity, CancellationToken ct)
        {
            const string sql = @"
                INSERT INTO ProjectProgresses (
                    Id,
                    ProjectId,
                    TotalWordsWritten,
                    RemainingWords,
                    RemainingPercentage,
                    CreatedAt,
                    Date,
                    TimeSpentInMinutes,
                    WordsWritten,
                    Notes,
                    Minutes,
                    Pages
                )
                VALUES (
                    @Id,
                    @ProjectId,
                    @TotalWordsWritten,
                    @RemainingWords,
                    @RemainingPercentage,
                    @CreatedAt,
                    @Date,
                    @TimeSpentInMinutes,
                    @WordsWritten,
                    @Notes,
                    @Minutes,
                    @Pages
                );";

            await db.ExecuteAsync(
                sql,
                new
                {
                    entity.Id,
                    entity.ProjectId,
                    entity.TotalWordsWritten,
                    entity.RemainingWords,
                    entity.RemainingPercentage,
                    entity.CreatedAt,
                    entity.Date,
                    entity.TimeSpentInMinutes,
                    entity.WordsWritten,
                    entity.Notes,
                    entity.Minutes,
                    entity.Pages
                },
                ct
            );

            return entity;
        }
        
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

            var affected = await db.ExecuteAsync(sql, new { id, userId });
            return affected > 0;
        }
        
        public Task<IReadOnlyList<ProjectProgress>> GetByProjectAndDateRangeAsync(Guid projectId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
        {
            const string sql = @"
                SELECT
                    Id,
                    ProjectId,
                    TotalWordsWritten,
                    RemainingWords,
                    RemainingPercentage,
                    CreatedAt,
                    [Date],
                    TimeSpentInMinutes,
                    WordsWritten,
                    Notes,
                    Minutes,
                    Pages
                FROM ProjectProgresses
                WHERE ProjectId = @ProjectId
                  AND CreatedAt >= @StartUtc
                  AND CreatedAt <  @EndUtc;
            ";

            return db.QueryAsync<ProjectProgress>(
                sql,
                new
                {
                    ProjectId = projectId,
                    StartUtc = startUtc,
                    EndUtc = endUtc
                },
                ct
            );
        }
        
    }
}
