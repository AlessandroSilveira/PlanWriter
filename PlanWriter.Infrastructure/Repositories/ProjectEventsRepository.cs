using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class ProjectEventsRepository(AppDbContext context) : Repository<ProjectEvent>(context), IProjectEventsRepository
{
    public async Task<ProjectEvent?> GetProjectEventByProjectIdAndEventId(Guid reqProjectId, Guid reqEventId)
    {
        return await _dbSet.
            Include(x => x.Event).
            FirstOrDefaultAsync(a=>a.ProjectId == reqProjectId && a.EventId == reqEventId);
    }

    public async Task<ProjectEvent> AddProjectEvent(ProjectEvent pe)
    {
        await _dbSet.AddAsync(pe);
        await _context.SaveChangesAsync();

        return pe;
    }

    public async Task<ProjectEvent?> GetProjectEventByProjectId(Guid projectEventId)
    {
        return await _dbSet.
            Include(x => x.Event).
            FirstOrDefaultAsync(a=>a.ProjectId == projectEventId);

    }
}