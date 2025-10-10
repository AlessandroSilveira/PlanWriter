// PlanWriter.Infrastructure/Repositories/UserFollowRepository.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class UserFollowRepository(AppDbContext db) : IUserFollowRepository
{
    public Task<bool> ExistsAsync(Guid followerId, Guid followeeId, CancellationToken ct) =>
        db.UserFollows.AnyAsync(x => x.FollowerId == followerId && x.FolloweeId == followeeId, ct);

    public async Task AddAsync(UserFollow follow, CancellationToken ct)
    {
        db.UserFollows.Add(follow);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid followerId, Guid followeeId, CancellationToken ct)
    {
        var entity = await db.UserFollows
            .FirstOrDefaultAsync(x => x.FollowerId == followerId && x.FolloweeId == followeeId, ct);
        if (entity is not null)
        {
            db.UserFollows.Remove(entity);
            await db.SaveChangesAsync(ct);
        }
    }

    public Task<List<Guid>> GetFolloweeIdsAsync(Guid followerId, CancellationToken ct) =>
        db.UserFollows
            .Where(x => x.FollowerId == followerId)
            .Select(x => x.FolloweeId)
            .ToListAsync(ct);
}