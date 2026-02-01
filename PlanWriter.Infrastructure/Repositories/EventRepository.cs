using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class EventRepository(AppDbContext context) : Repository<Event>(context), IEventRepository
{
    public async Task<List<EventDto>> GetActiveEvents()
    {
        try
        {
            var now = DateTime.UtcNow;
            var q = await DbSet
                .Where(e => e.IsActive && e.StartsAtUtc <= now && e.EndsAtUtc >= now)
                .Select(e => new EventDto(e.Id, e.Name, e.Slug, e.Type.ToString(),
                    e.StartsAtUtc, e.EndsAtUtc, e.DefaultTargetWords, e.IsActive))
                .ToArrayAsync();
            return q.ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public Task<bool> GetEventBySlug(string reqSlug)
    {
        try
        {
            return DbSet.AnyAsync(e => e.Slug == reqSlug);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task AddEvent(Event ev)
    {
        try
        {
            DbSet.Add(ev);
            await Context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<Event?> GetEventById(Guid reqEventId)
    {
        try
        {
            return await DbSet.FirstOrDefaultAsync(e => e.Id == reqEventId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<EventDto>?> GetAllAsync()
    {
        try
        {
            return await DbSet.Select(e => new EventDto(e.Id, e.Name, e.Slug, e.Type.ToString(), e.StartsAtUtc, e.EndsAtUtc, e.DefaultTargetWords, e.IsActive)).ToListAsync();

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task UpdateAsync(Event ev, Guid id)
    {
        try
        {
            DbSet.Update(ev);
            await Context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public async Task DeleteAsync(Event ev)
    {
        try
        {
            DbSet.Remove(ev);
            await Context.SaveChangesAsync();
            return;
        }catch(Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
       
    }

    public async Task<List<MyEventDto>> GetEventByUserId(Guid userId)
    {
        try
        {
            var query =
                from pe in context.ProjectEvents
                join e in DbSet on pe.EventId equals e.Id
                where pe.Project!.UserId == userId
                select new MyEventDto
                {
                    ProjectId = pe.ProjectId,
                    EventId = pe.EventId,
                    EventName = e.Name,
                    ProjectTitle = pe.Project.Title,
                    TotalWrittenInEvent = e.DefaultTargetWords,
                    TargetWords = pe.TargetWords
                };

            return await query
                .Distinct()
                .ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<EventLeaderboardRowDto>> GetLeaderboard(
        Event ev,
        DateTime winStart,
        DateTime winEnd,
        int top)
    {
        try
        {
            // ===============================
    // 1. AGREGA PROGRESSO (SQL)
    // ===============================
    var progressAgg = await context.ProjectProgresses
        .Where(p =>
            p.CreatedAt >= winStart &&
            p.CreatedAt < winEnd.AddDays(1))
        .GroupBy(p => p.ProjectId)
        .Select(g => new
        {
            ProjectId = g.Key,
            Words = g.Sum(x => x.WordsWritten)
        })
        .ToListAsync();

    // ===============================
    // 2. BASE DO LEADERBOARD (SQL)
    // ===============================
    var baseRows = await (
        from pe in context.ProjectEvents
        join p in context.Projects
            on pe.ProjectId equals p.Id
        join u in context.Users
            on p.UserId equals u.Id
        where pe.EventId == ev.Id
        select new
        {
            pe.ProjectId,
            pe.TargetWords,
            ProjectTitle = p.Title,
            UserName = u.FirstName + " " + u.LastName
        }
    ).ToListAsync();

    // ===============================
    // 3. MERGE + CÁLCULO (MEMÓRIA)
    // ===============================
    var rows = baseRows
        .Select(r =>
        {
            var agg = progressAgg.FirstOrDefault(x => x.ProjectId == r.ProjectId);
            var words = agg?.Words ?? 0;
            var target = r.TargetWords ?? ev.DefaultTargetWords ?? 0;

            return new EventLeaderboardRowDto
            {
                ProjectId = r.ProjectId.Value,
                ProjectTitle = r.ProjectTitle,
                UserName = r.UserName,
                Words = words,
                Percent = target > 0
                    ? (double)words / target * 100
                    : 0,
                Won = target > 0 && words >= target,
                Rank = 0
            };
        })
        .OrderByDescending(r => r.Words)
        .ThenBy(r => r.ProjectTitle)
        .Take(Math.Clamp(top, 1, 200))
        .ToList();

    // ===============================
    // 4. RANK
    // ===============================
    for (int i = 0; i < rows.Count; i++)
        rows[i].Rank = i + 1;

    return rows;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }





}