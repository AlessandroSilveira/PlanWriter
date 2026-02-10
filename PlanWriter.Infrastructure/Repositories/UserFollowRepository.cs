// PlanWriter.Infrastructure/Repositories/UserFollowRepository.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class UserFollowRepository(IDbExecutor db) : IUserFollowRepository
{
    public async Task<bool> ExistsAsync(Guid followerId, Guid followeeId, CancellationToken ct)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM UserFollows
            WHERE FollowerId = @FollowerId
              AND FolloweeId = @FolloweeId;
        ";

        var count = await db.QueryFirstOrDefaultAsync<int>(
            sql,
            new { FollowerId = followerId, FolloweeId = followeeId },
            ct
        );

        return count > 0;
    }

    public Task AddAsync(UserFollow follow, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO UserFollows
            (
                FollowerId,
                FolloweeId,
                CreatedAtUtc
            )
            VALUES
            (
                @FollowerId,
                @FolloweeId,
                @CreatedAtUtc
            );
        ";

        return db.ExecuteAsync(sql, new
        {
            follow.FollowerId,
            follow.FolloweeId,
            follow.CreatedAtUtc
        }, ct);
    }

    public Task RemoveAsync(Guid followerId, Guid followeeId, CancellationToken ct)
    {
        const string sql = @"
            DELETE FROM UserFollows
            WHERE FollowerId = @FollowerId
              AND FolloweeId = @FolloweeId;
        ";

        return db.ExecuteAsync(sql, new { FollowerId = followerId, FolloweeId = followeeId }, ct);
    }

    public async Task<List<Guid>> GetFolloweeIdsAsync(Guid followerId, CancellationToken ct)
    {
        const string sql = @"
            SELECT FolloweeId
            FROM UserFollows
            WHERE FollowerId = @FollowerId;
        ";

        var rows = await db.QueryAsync<Guid>(sql, new { FollowerId = followerId }, ct);
        return rows.ToList();
    }
}
