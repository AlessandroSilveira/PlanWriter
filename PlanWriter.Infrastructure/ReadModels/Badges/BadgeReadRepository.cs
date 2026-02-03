using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Badges;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Badges;

public class BadgeReadRepository(IDbExecutor db) : IBadgeReadRepository
{
    public Task<IReadOnlyList<Badge>> GetByProjectIdAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                b.Id,
                b.Name,
                b.Description,
                b.Icon,
                b.AwardedAt,
                b.ProjectId,
                b.EventId
            FROM Badges b
            INNER JOIN Projects p ON p.Id = b.ProjectId
            WHERE b.ProjectId = @ProjectId
              AND p.UserId = @UserId
            ORDER BY b.AwardedAt DESC, b.Id DESC;
        ";

        return db.QueryAsync<Badge>(
            sql,
            new { ProjectId = projectId, UserId = userId },
            ct
        );
    }

    public async Task<bool> HasFirstStepsBadgeAsync(Guid projectId, CancellationToken ct)
    {
        const string sql = @"
            SELECT 1
            FROM Badges
            WHERE ProjectId = @ProjectId
              AND Name = @Name;
        ";

        var result = await db.QueryFirstOrDefaultAsync<int?>(sql,
            new
            {
                ProjectId = projectId,
                Name = "First Steps"
            },
            ct
        );

        return result.HasValue;
    }

    public async Task<bool> ExistsAsync(Guid projectId, Guid eventId, string name, CancellationToken ct)
    {
        const string sql = @"
            SELECT 1
            FROM Badges
            WHERE ProjectId = @ProjectId
              AND EventId = @EventId
              AND Name LIKE @Name;
        ";

        var result = await db.QueryFirstOrDefaultAsync<int?>(
            sql,
            new
            {
                ProjectId = projectId,
                EventId = eventId,
                Name = $"%{name}%"
            },
            ct
        );

        return result.HasValue;
    }
}