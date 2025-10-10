// PlanWriter.Domain/Interfaces/IUserFollowRepository.cs

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IUserFollowRepository
{
    Task<bool> ExistsAsync(Guid followerId, Guid followeeId, CancellationToken ct);
    Task AddAsync(UserFollow follow, CancellationToken ct);
    Task RemoveAsync(Guid followerId, Guid followeeId, CancellationToken ct);
    Task<List<Guid>> GetFolloweeIdsAsync(Guid followerId, CancellationToken ct);
}