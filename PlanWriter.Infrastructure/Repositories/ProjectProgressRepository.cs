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

            await connection.ExecuteAsync(
                new CommandDefinition(
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
                    cancellationToken: ct
                )
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

            var affected = await connection.ExecuteAsync(sql, new { id, userId });
            return affected > 0;
        }
        
    }
}