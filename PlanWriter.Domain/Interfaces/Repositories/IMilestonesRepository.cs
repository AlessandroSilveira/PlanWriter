using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IMilestonesRepository
{
    Task CreateAsync(Milestone milestone, CancellationToken ct);
    Task UpdateAsync(Milestone milestone, CancellationToken ct);
    Task DeleteAsync(Guid milestoneId, Guid userId, CancellationToken ct);
}