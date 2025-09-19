using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Services;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Application.Services;

public class EventService : IEventService
{
    private readonly AppDbContext _db;

    public EventService(AppDbContext db) => _db = db;

    public async Task<EventDto[]> GetActiveAsync()
    {
        var now = DateTime.UtcNow;
        var q = await _db.Events
            .Where(e => e.IsActive && e.StartsAtUtc <= now && e.EndsAtUtc >= now)
            .Select(e => new EventDto(e.Id, e.Name, e.Slug, e.Type.ToString(),
                e.StartsAtUtc, e.EndsAtUtc, e.DefaultTargetWords, e.IsActive))
            .ToArrayAsync();
        return q;
    }

    public async Task<EventDto> CreateAsync(CreateEventRequest req)
    {
        if (await _db.Events.AnyAsync(e => e.Slug == req.Slug))
            throw new InvalidOperationException("Slug já está em uso.");

        var type = Enum.TryParse<EventType>(req.Type, true, out var t) ? t : EventType.Custom;

        var ev = new Event
        {
            Name = req.Name.Trim(),
            Slug = req.Slug.Trim().ToLowerInvariant(),
            Type = type,
            StartsAtUtc = DateTime.SpecifyKind(req.StartsAtUtc, DateTimeKind.Utc),
            EndsAtUtc   = DateTime.SpecifyKind(req.EndsAtUtc,   DateTimeKind.Utc),
            DefaultTargetWords = req.DefaultTargetWords,
            IsActive = true
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return new EventDto(ev.Id, ev.Name, ev.Slug, ev.Type.ToString(),
            ev.StartsAtUtc, ev.EndsAtUtc, ev.DefaultTargetWords, ev.IsActive);
    }

    public async Task<ProjectEvent> JoinAsync(JoinEventRequest req)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == req.EventId)
                 ?? throw new KeyNotFoundException("Evento não encontrado.");
        // Valide existência do projeto:
        var projectExists = await _db.Set<Project>().AnyAsync(p => p.Id == req.ProjectId);
        if (!projectExists) throw new KeyNotFoundException("Projeto não encontrado.");

        var existing = await _db.ProjectEvents
            .FirstOrDefaultAsync(pe => pe.ProjectId == req.ProjectId && pe.EventId == req.EventId);
        if (existing != null) return existing;

        var pe = new ProjectEvent
        {
            ProjectId = req.ProjectId,
            EventId   = req.EventId,
            TargetWords = req.TargetWords ?? ev.DefaultTargetWords
        };
        _db.ProjectEvents.Add(pe);
        await _db.SaveChangesAsync();
        return pe;
    }

    // Application/Events/EventService.cs  (método GetProgressAsync)
    public async Task<EventProgressDto> GetProgressAsync(Guid projectId, Guid eventId)
    {
        var pe = await _db.ProjectEvents
                     .Include(x => x.Event)
                     .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.EventId == eventId)
                 ?? throw new KeyNotFoundException("Inscrição do projeto no evento não encontrada.");

        var ev = pe.Event!;
        var target = pe.TargetWords ?? ev.DefaultTargetWords ?? 50000;

        var totalInEvent = await _db.Set<ProjectProgress>()
            .Where(w => w.ProjectId == projectId
                        && w.CreatedAt >= ev.StartsAtUtc
                        && w.CreatedAt <  ev.EndsAtUtc)
            .SumAsync(w => (int?)w.WordsWritten) ?? 0;

        var days = Math.Max(1, (int)Math.Ceiling((ev.EndsAtUtc - ev.StartsAtUtc).TotalDays));
        var nowIndex = Math.Clamp(
            (int)Math.Ceiling((DateTime.UtcNow - ev.StartsAtUtc).TotalDays), 1, days);

        var percent   = (int)Math.Min(100, Math.Round(100.0 * totalInEvent / Math.Max(1, target)));
        var remaining = Math.Max(0, target - totalInEvent);
        var dailyTarget = (int)Math.Ceiling((double)target / days);

        return new EventProgressDto(
            projectId, eventId, target, totalInEvent, percent, remaining, days, nowIndex, dailyTarget,
            pe.Id, pe.ValidatedAtUtc, pe.Won
        );
    }


    public async Task<ProjectEvent> FinalizeAsync(Guid projectEventId)
    {
        var pe = await _db.ProjectEvents.Include(x => x.Event)
                     .FirstOrDefaultAsync(x => x.Id == projectEventId)
                 ?? throw new KeyNotFoundException("Inscrição não encontrada.");

        var ev = pe.Event!;
        if (DateTime.UtcNow < ev.EndsAtUtc.AddHours(-1))
            throw new InvalidOperationException("Validação só disponível ao final do evento.");

        var totalInEvent = await _db.Set<ProjectProgress>()
            .Where(w => w.ProjectId == pe.ProjectId
                        && w.CreatedAt >= ev.StartsAtUtc
                        && w.CreatedAt <  ev.EndsAtUtc)
            .SumAsync(w => (int?)w.WordsWritten) ?? 0;

        pe.FinalWordCount = totalInEvent;
        pe.ValidatedAtUtc = DateTime.UtcNow;
        pe.Won = pe.FinalWordCount >= (pe.TargetWords ?? ev.DefaultTargetWords ?? 50000);

        // Premia
        var already = await _db.Badges
            .AnyAsync(b => b.ProjectId == pe.ProjectId && b.EventId == ev.Id && b.Name.Contains(ev.Name));
        if (!already)
        {
            var badge = new Badge
            {
                ProjectId = pe.ProjectId,
                EventId = ev.Id,
                Name = pe.Won ? $"Winner — {ev.Name}" : $"Participant — {ev.Name}",
                Description = pe.Won ? "Meta atingida no evento" : "Participou do evento",
                Icon = pe.Won ? "🏆" : "🎉"
            };
            _db.Badges.Add(badge);
        }

        await _db.SaveChangesAsync();
        return pe;
    }

}