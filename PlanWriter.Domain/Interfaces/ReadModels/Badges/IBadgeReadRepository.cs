using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.ReadModels.Badges;

public interface IBadgeReadRepository
{
    Task<IReadOnlyList<Badge>> GetByProjectIdAsync(Guid projectId, Guid userId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid projectId, Guid eventId, string name, CancellationToken ct);
    Task<bool> HasFirstStepsBadgeAsync(Guid projectId, CancellationToken ct);
}