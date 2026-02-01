using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class BadgeRepository(AppDbContext context) : IBadgeRepository
{
    public async Task<IEnumerable<Badge>> GetByProjectIdAsync(Guid projectId)
    {
        return await context.Set<Badge>()
            .Where(b => b.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<bool> HasFirstStepsBadge(Guid projectId)
    {
        return await context.Set<Badge>()
            .AnyAsync(b =>
                b.ProjectId == projectId &&
                b.Name == "First Steps");
    }

    public async Task<bool> ExistsAsync(
        Guid projectId,
        Guid eventId,
        string name)
    {
        return await context.Set<Badge>()
            .AnyAsync(b =>
                b.ProjectId == projectId &&
                b.EventId == eventId &&
                b.Name.Contains(name));
    }

    public async Task SaveAsync(IEnumerable<Badge> badges)
    {
        await context.Set<Badge>().AddRangeAsync(badges);
        await context.SaveChangesAsync();
    }
}