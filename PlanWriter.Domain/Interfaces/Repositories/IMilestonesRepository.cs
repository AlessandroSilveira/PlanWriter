using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IMilestonesRepository
{
    Task<List<Milestone>> GetByProjectIdAsync(Guid projectId);

    Task<Milestone> AddAsync(Milestone milestone);

    Task DeleteAsync(Guid milestoneId, Guid userId);

    Task<int> GetNextOrderAsync(Guid projectId);
    Task<bool> ExistsAsync(Guid projectId, string name, CancellationToken ct);
    Task UpdateAsync(Milestone milestone, CancellationToken ct);
}