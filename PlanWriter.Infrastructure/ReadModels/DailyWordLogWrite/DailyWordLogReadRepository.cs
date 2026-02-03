using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.DailyWordLogWrite;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.DailyWordLogWrite;

public sealed class DailyWordLogReadRepository(IDbExecutor db) : IDailyWordLogReadRepository
{
    public Task<IReadOnlyList<DailyWordLogDto>> GetByProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken ct
    )
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

        return db.QueryAsync<DailyWordLogDto>(
            sql,
            new { ProjectId = projectId, UserId = userId },
            ct
        );
    }
}