using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.ReadModels;

namespace PlanWriter.Infrastructure.ReadModels.Projects;

public class ProjectReadRepository(IConfiguration configuration) : IProjectReadRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection");

    public async Task<List<ProjectDto>> GetUserProjectsAsync(Guid userId)
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
                CASE WHEN CoverBytes IS NULL THEN 0 ELSE 1 END AS HasCover
            FROM Projects
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC
        ";

        await using var conn = new SqlConnection(_connectionString);

        var result = await conn.QueryAsync<ProjectDto>(
            sql,
            new { UserId = userId }
        );

        return result.ToList();
    }
    
    public async Task<ProjectDto?> GetProjectByIdAsync(Guid projectId, Guid userId)
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

        using var conn = new SqlConnection(_connectionString);

        return await conn.QueryFirstOrDefaultAsync<ProjectDto>(
            sql,
            new
            {
                ProjectId = projectId,
                UserId = userId
            }
        );
    }

}