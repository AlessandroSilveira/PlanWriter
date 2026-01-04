using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class EventRepository(AppDbContext context) : Repository<Event>(context), IEventRepository
{
    public async Task<EventDto[]> GetActiveEvents()
    {
        var now = DateTime.UtcNow;
        var q = await DbSet
            .Where(e => e.IsActive && e.StartsAtUtc <= now && e.EndsAtUtc >= now)
            .Select(e => new EventDto(e.Id, e.Name, e.Slug, e.Type.ToString(),
                e.StartsAtUtc, e.EndsAtUtc, e.DefaultTargetWords, e.IsActive))
            .ToArrayAsync();
        return q;
    }

    public Task<bool> GetEventBySlug(string reqSlug)
    {
        return DbSet.AnyAsync(e => e.Slug == reqSlug);
    }

    public async Task AddEvent(Event ev)
    {
        DbSet.Add(ev);
        await Context.SaveChangesAsync();
    }

    public async Task<Event?> GetEventById(Guid reqEventId)
    {
        return await DbSet.FirstOrDefaultAsync(e => e.Id == reqEventId);
    }
}