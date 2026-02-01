using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IBadgeRepository
{
    Task<IEnumerable<Badge>> GetByProjectIdAsync(Guid projectId);
    Task<bool> HasFirstStepsBadge(Guid projectId);
    Task SaveAsync(IEnumerable<Badge> badges);
    Task<bool> ExistsAsync(Guid projectId, Guid eventId, string name);
}
