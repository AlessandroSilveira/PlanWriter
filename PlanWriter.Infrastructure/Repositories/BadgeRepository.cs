using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class BadgeRepository(IDbExecutor db) : IBadgeRepository
{
    public Task SaveAsync(IEnumerable<Badge> badges)
    {
        const string sql = @"
            INSERT INTO Badges
            (
                Name,
                Description,
                Icon,
                AwardedAt,
                ProjectId,
                EventId
            )
            VALUES
            (
                @Name,
                @Description,
                @Icon,
                @AwardedAt,
                @ProjectId,
                @EventId
            );
        ";

        var list = badges as IReadOnlyList<Badge> ?? badges.ToList();
        return list.Count == 0 ? Task.CompletedTask : db.ExecuteAsync(sql, list);
    }
}
