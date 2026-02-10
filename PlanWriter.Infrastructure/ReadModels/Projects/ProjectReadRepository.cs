using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Infrastructure.Data;


namespace PlanWriter.Infrastructure.ReadModels.Projects;

public class ProjectReadRepository(IDbExecutor db) : IProjectReadRepository
{
    public Task<IReadOnlyList<ProjectDto>> GetUserProjectsAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                Id,
                Title,
                Description,
                CurrentWordCount,
                WordCountGoal,
                Deadline,
                Genre,
                StartDate,
                CoverUpdatedAt,
                GoalUnit,
                CASE WHEN CoverBytes IS NULL THEN 0 ELSE 1 END AS HasCover,
                IsPublic
            FROM Projects
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC;
        ";

        return db.QueryAsync<ProjectDto>(sql, new { UserId = userId }, ct);
    }

    public Task<ProjectDto?> GetProjectByIdAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                Id,
                Title,
                Description,
                CurrentWordCount,
                WordCountGoal,
                Deadline,
                Genre,
                StartDate,
                CoverUpdatedAt,
                GoalUnit,
                CASE
                    WHEN CoverBytes IS NULL THEN CAST(0 AS bit)
                    ELSE CAST(1 AS bit)
                END AS HasCover
            FROM Projects
            WHERE Id = @ProjectId
              AND UserId = @UserId;
        ";

        return db.QueryFirstOrDefaultAsync<ProjectDto>(sql, new { ProjectId = projectId, UserId = userId }, ct);
    }

    public Task<Project?> GetUserProjectByIdAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP 1
                p.Id,
                p.UserId,
                p.Title,
                p.Description,
                p.CurrentWordCount,
                p.WordCountGoal,
                p.GoalAmount,
                p.GoalUnit,
                p.StartDate,
                p.Deadline,
                p.Genre,
                p.CoverUpdatedAt
            FROM Projects p
            WHERE p.Id = @ProjectId
              AND p.UserId = @UserId;
        ";

        return db.QueryFirstOrDefaultAsync<Project>(sql, new { ProjectId = projectId, UserId = userId }, ct);
    }
}
