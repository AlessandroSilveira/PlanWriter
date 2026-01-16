using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Buddies;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IBuddiesService
{
    Task FollowByUsernameAsync(Guid me, string Email, CancellationToken ct);
    Task FollowByIdAsync(Guid me, Guid followeeId, CancellationToken ct);
    Task UnfollowAsync(Guid me, Guid followeeId, CancellationToken ct);
    Task<List<BuddiesDto.BuddySummaryDto>> ListAsync(Guid me, CancellationToken ct);
    Task<List<BuddiesDto.BuddyLeaderboardItemDto>> LeaderboardAsync(Guid me, Guid? eventId, DateOnly? start, DateOnly? end, CancellationToken ct);
    Task<List<BuddyLeaderboardRowDto>> GetBuddiesLeaderboardAsync(Guid userId, DateTime? start, DateTime? end);
}