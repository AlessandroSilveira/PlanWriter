using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.Repositories.DailyWordLogWrite;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.DailyWordWrite;

public class DailyWordLogWriteRepository(IDbExecutor db) : IDailyWordLogWriteRepository
{
    public async Task UpsertAsync(Guid projectId, Guid userId, DateOnly date, int wordsWritten, CancellationToken ct)
    {
        const string updateSql = @"
            UPDATE DailyWordLogs
            SET WordsWritten = @WordsWritten
            WHERE ProjectId = @ProjectId
              AND UserId    = @UserId
              AND [Date]    = @Date;
        ";

        var affected = await db.ExecuteAsync(
            updateSql,
            new
            {
                ProjectId = projectId,
                UserId = userId,
                Date = date,           // 👈 DateOnly direto
                WordsWritten = wordsWritten
            },
            ct
        );

        if (affected > 0)
            return;

        const string insertSql = @"
            INSERT INTO DailyWordLogs
                (Id, ProjectId, UserId, [Date], WordsWritten)
            VALUES
                (@Id, @ProjectId, @UserId, @Date, @WordsWritten);
        ";

        await db.ExecuteAsync(insertSql,
            new
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = userId,
                Date = date,           
                WordsWritten = wordsWritten
            }, ct);
    }
    
    public Task<IReadOnlyList<DailyWordLogDto>> GetByProjectAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                l.[Date]       AS [Date],
                l.WordsWritten AS WordsWritten
            FROM DailyWordLogs l
            WHERE l.ProjectId = @ProjectId
              AND l.UserId    = @UserId
            ORDER BY l.[Date] ASC;
        ";

        return db.QueryAsync<DailyWordLogDto>(sql, new { ProjectId = projectId, UserId = userId }, ct);
    }
}