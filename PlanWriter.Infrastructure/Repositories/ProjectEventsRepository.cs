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
        return await DbSet
            .Include(x => x.Event)
            .FirstOrDefaultAsync(a => a.ProjectId == reqProjectId && a.EventId == reqEventId);
    }

    public async Task<ProjectEvent> AddProjectEvent(ProjectEvent pe)
    {
        await DbSet.AddAsync(pe);
        await Context.SaveChangesAsync();
        return pe;
    }

    public async Task<ProjectEvent?> GetProjectEventByProjectId(Guid projectId)
    {
        return await DbSet
            .Include(x => x.Event)
            .FirstOrDefaultAsync(a => a.ProjectId == projectId);
    }

    // ✅ novo: atualizar (ex.: meta/TargetWords)
    public async Task UpdateProjectEvent(ProjectEvent pe)
    {
        DbSet.Update(pe);
        await Context.SaveChangesAsync();
    }

    // ✅ novo: remover inscrição por chaves
    public async Task<bool> RemoveByKeys(Guid projectId, Guid eventId)
    {
        var row = await DbSet.FirstOrDefaultAsync(a => a.ProjectId == projectId && a.EventId == eventId);
        if (row is null) return false;
        DbSet.Remove(row);
        await Context.SaveChangesAsync();
        return true;
    }
    // ✅ novo
    public async Task<ProjectEvent?> GetProjectEventById(Guid projectEventId)
    {
        return await DbSet
            .Include(pe => pe.Event)
            .FirstOrDefaultAsync(pe => pe.Id == projectEventId);
    }

   

    
}