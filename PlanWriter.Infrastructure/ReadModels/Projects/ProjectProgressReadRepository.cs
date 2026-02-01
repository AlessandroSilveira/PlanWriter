using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels;

namespace PlanWriter.Infrastructure.ReadModels.Projects;

public class ProjectProgressReadRepository(IConfiguration configuration, IDbConnection connection) : IProjectProgressReadRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")!;

    public async Task<IEnumerable<ProgressHistoryRow>> GetProgressHistoryAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        const string sql = """
                               SELECT
                                   pp.[Date]        AS [Date],
                                   pp.WordsWritten  AS WordsWritten
                               FROM ProjectProgresses pp
                               INNER JOIN Projects p ON p.Id = pp.ProjectId
                               WHERE pp.ProjectId = @ProjectId
                                 AND p.UserId = @UserId
                               ORDER BY pp.[Date] ASC;
                           """;

        var cmd = new CommandDefinition(
            sql,
            new { ProjectId = projectId, UserId = userId },
            cancellationToken: ct
        );

        return await connection.QueryAsync<ProgressHistoryRow>(cmd);
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

        var cmd = new CommandDefinition(
            sql,
            new { UserId = userId, StartUtc = startUtc, EndUtc = endUtc },
            cancellationToken: ct
        );

        return await connection.ExecuteScalarAsync<int>(cmd);
    }

    public async Task<int> GetMonthlyWordsAsync(Guid userId, DateTime start, DateTime end)
    {
        const string sql = @"
            SELECT
                COALESCE(SUM(WordsWritten), 0)
            FROM ProjectProgresses PP
            JOIN Projects P ON PP.ProjectId = P.ID
            WHERE PP.ProjectId = @ProjectId              
              AND Date >= @Start
              AND Date <  @End;
        ";

        await using var conn = new SqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<int>(
            sql,
            new
            {
                UserId = userId,
                Start = start,
                End = end
            }
        );
    }

    public async Task<IEnumerable<ProjectProgressDayDto>> GetProgressByDayAsync(Guid projectId, Guid userId)
    {
        const string sql = @"
        SELECT
            CAST(Date AS date) AS [Date],
            SUM(WordsWritten) AS TotalWords,
            SUM(Minutes) AS TotalMinutes,
            SUM(Pages) AS TotalPages
        FROM ProjectProgresses PP
            JOIN Projects P ON PP.ProjectId = P.ID
            WHERE PP.ProjectId = @ProjectId  
          AND UserId = @UserId
        GROUP BY CAST(Date AS date)
        ORDER BY CAST(Date AS date);
    ";

        await using var conn = new SqlConnection(_connectionString);

        return await conn.QueryAsync<ProjectProgressDayDto>(
            sql,
            new { ProjectId = projectId, UserId = userId }
        );
    }
    
    public async Task<ProgressRow?> GetByIdAsync(Guid progressId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                pp.Id,
                pp.ProjectId,
                pp.[Date]
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.Id = @progressId
              AND p.UserId = @userId;
            ";
       
        await using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ProgressRow>(sql, new { progressId, userId });
    }
    
    public async Task<ProgressRow?> GetLastProgressBeforeAsync(Guid projectId, Guid userId, DateTime date, CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP 1
                pp.TotalWordsWritten
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.ProjectId = @projectId
              AND p.UserId = @userId
              AND pp.[Date] < @date
            ORDER BY
                pp.[Date] DESC,
                pp.CreatedAt DESC,
                pp.Id DESC;";

        var cmd = new CommandDefinition(
            sql,
            new { projectId, userId, date },
            cancellationToken: ct
        );

        
        await using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ProgressRow>(cmd);
    }
    
    public async Task<int> GetLastTotalBeforeAsync(Guid projectId, Guid userId, DateTime date, CancellationToken ct)
    {
        
        const string sql = @"
            SELECT TOP 1
                pp.TotalWordsWritten
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE pp.ProjectId = @projectId
              AND p.UserId = @userId
              AND pp.[Date] < @date
            ORDER BY 
                pp.[Date] DESC,
                pp.Id DESC;
            ";

        var cmd = new CommandDefinition(sql, new { projectId, userId, date }, cancellationToken: ct);
        await using var conn = new SqlConnection(_connectionString);
        var lastTotal = await conn.QueryFirstOrDefaultAsync<int?>(cmd);
        return lastTotal ?? 0;
    }
    
    
}