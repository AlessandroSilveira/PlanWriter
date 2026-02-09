using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.ReadModels.Milestones;

public interface IMilestonesReadRepository
{
    Task<IReadOnlyList<Milestone>> GetByProjectIdAsync(Guid projectId, CancellationToken ct);
    Task<int> GetNextOrderAsync(Guid projectId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid projectId, string name, CancellationToken ct);
}