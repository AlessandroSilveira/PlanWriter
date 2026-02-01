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

public class MilestonesRepository(AppDbContext ctx) : IMilestonesRepository
{
    public async Task<List<Milestone>> GetByProjectIdAsync(Guid projectId)
    {
        return await ctx.Milestones
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.Order)
            .ToListAsync();
    }

    public async Task<Milestone> AddAsync(Milestone milestone)
    {
        ctx.Milestones.Add(milestone);
        await ctx.SaveChangesAsync();
        return milestone;
    }

    public async Task DeleteAsync(Guid milestoneId, Guid userId)
    {
        var m = await ctx.Milestones
            .Where(x => x.Id == milestoneId)
            .FirstOrDefaultAsync();

        if (m == null)
            return;

        ctx.Milestones.Remove(m);
        await ctx.SaveChangesAsync();
    }

    public async Task<int> GetNextOrderAsync(Guid projectId)
    {
        var lastOrder = await ctx.Milestones
            .Where(m => m.ProjectId == projectId)
            .MaxAsync(m => (int?)m.Order);

        return (lastOrder ?? 0) + 1;
    }
    public async Task<bool> ExistsAsync(Guid projectId, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var normalized = name.Trim();

        return await ctx.Milestones
            .AsNoTracking()
            .AnyAsync(m => m.ProjectId == projectId && m.Name == normalized, ct);
    }
    
    public async Task UpdateAsync(Milestone milestone, CancellationToken ct)
    {
        ctx.Milestones.Update(milestone);
        await ctx.SaveChangesAsync(ct);
    }

}