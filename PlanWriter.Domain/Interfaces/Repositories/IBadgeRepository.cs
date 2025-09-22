using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IBadgeRepository
{
    Task<bool> HasFirstStepsBadge(Guid projectId);
    Task SaveBadges(List<Badge> badges);
    Task<IEnumerable<Badge>> GetBadgesByProjectIdAsync(Guid projectId);
    Task<bool> FindAsync(Func<object, bool> func);
    Task<bool> FindAsync(Guid projectId, Guid id, string evName);
}