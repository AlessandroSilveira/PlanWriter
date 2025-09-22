using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;
using PlanWriter.Infrastructure.Repositories;

namespace PlanWriter.Application.Services;

public class EventService
    (
        IEventRepository eventRepository, 
        ProjectRepository projectRepository,
        IProjectEventsRepository projectEventsRepository,
        IProjectProgressRepository projectProgressRepository,
        IBadgeRepository badgeRepository
    ) : IEventService
{
    public async Task<EventDto[]> GetActiveAsync()
    {
        return await eventRepository.GetActiveEvents();
    }

    public async Task<EventDto> CreateAsync(CreateEventRequest req)
    {
        
        var eventSlug = await eventRepository.GetEventBySlug(req.Slug);
        
        if (eventSlug)
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

        await eventRepository.AddEvent(ev);
        
        return new EventDto(ev.Id, ev.Name, ev.Slug, ev.Type.ToString(),
            ev.StartsAtUtc, ev.EndsAtUtc, ev.DefaultTargetWords, ev.IsActive);
    }

    public async Task<ProjectEvent> JoinAsync(JoinEventRequest req)
    {
        var ev = await eventRepository.GetEventById(req.EventId)
                 ?? throw new KeyNotFoundException("Evento não encontrado.");
        
        // Valide existência do projeto:
        var projectExists = await projectRepository.GetProjectById(req.ProjectId);
        if (projectExists is null ) throw new KeyNotFoundException("Projeto não encontrado.");

        var existing = await projectEventsRepository.GetProjectEventByProjectIdAndEventId(req.ProjectId, req.EventId);
    
        if (existing != null) return existing;

        var pe = new ProjectEvent
        {
            ProjectId = req.ProjectId,
            EventId   = req.EventId,
            TargetWords = req.TargetWords ?? ev.DefaultTargetWords
        };

        return await projectEventsRepository.AddProjectEvent(pe);

    }

    
    public async Task<EventProgressDto> GetProgressAsync(Guid projectId, Guid eventId)
    {
        var pe = await projectEventsRepository.GetProjectEventByProjectIdAndEventId(projectId, eventId)
                 ?? throw new KeyNotFoundException("Inscrição do projeto no evento não encontrada.");

        var ev = pe.Event!;
        var target = pe.TargetWords ?? ev.DefaultTargetWords ?? 50000;

        var totalInEventList = await projectProgressRepository
            .FindAsync(w => w.ProjectId == projectId
                && w.CreatedAt >= ev.StartsAtUtc
                && w.CreatedAt < ev.EndsAtUtc);
        
        var totalInEvent = totalInEventList.Sum(w => (int?)w.WordsWritten) ?? 0;
       
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
        // var pe = await db.ProjectEvents.Include(x => x.Event)
        //              .FirstOrDefaultAsync(x => x.Id == projectEventId)
        //          ?? throw new KeyNotFoundException("Inscrição não encontrada.");
        
        var pe = await projectEventsRepository.GetProjectEventByProjectId(projectEventId);
        
        var ev = pe?.Event!;
        if (DateTime.UtcNow < ev.EndsAtUtc.AddHours(-1))
            throw new InvalidOperationException("Validação só disponível ao final do evento.");

        var totalInEventList = await projectProgressRepository
            .FindAsync(w => w.ProjectId == pe.ProjectId
                            && w.CreatedAt >= ev.StartsAtUtc
                            && w.CreatedAt < ev.EndsAtUtc);
        
        var totalInEvent = totalInEventList.Sum(w => (int?)w.WordsWritten) ?? 0;

        pe.FinalWordCount = totalInEvent;
        pe.ValidatedAtUtc = DateTime.UtcNow;
        pe.Won = pe.FinalWordCount >= (pe.TargetWords ?? ev.DefaultTargetWords ?? 50000);

        // Premia
        // var already = await db.Badges
        //     .

        var already = await badgeRepository
            .FindAsync(pe.ProjectId, ev.Id, ev.Name);
        
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
            badgeRepository.SaveBadges(new List<Badge> { badge });
           
        }
        
        return pe;
    }

    public async Task<List<EventLeaderboardRowDto>> GetLeaberBoard(Guid eventId, string scope, int top)
    {
        var ev = await eventRepository.GetEventById(eventId);
        if (ev == null) 
            throw new Exception("Evento nao encontrado.");

        var start = ev.StartsAtUtc.Date;
        var end = ev.EndsAtUtc.Date;
        var today = DateTime.UtcNow.Date;
        var effectiveEnd = today < end ? today : end;

        // janela válida do evento
        DateTime winStart = start, winEnd = effectiveEnd;

        // escopo "daily" = apenas hoje (se cair dentro da janela)
        bool daily = string.Equals(scope, "daily", StringComparison.OrdinalIgnoreCase);
        if (daily)
        {
            if (today < start || today > end)
                return []; // fora do período, nada a mostrar
            
            winStart = winEnd = today;
        }

        // somatório de palavras por projeto no intervalo
        // var agg = await db.ProjectProgresses
        //     .Where(w =>
        //         w.CreatedAt.Date >= winStart &&
        //         w.CreatedAt.Date <= winEnd)
        //     .GroupBy(w => w.ProjectId)
        //     .Select(g => new { ProjectId = g.Key, Words = g.Sum(x => x.WordsWritten) })
        //     .ToListAsync();

        var responseAgg = await projectProgressRepository.FindAsync(w =>
            w.CreatedAt.Date >= winStart &&
            w.CreatedAt.Date <= winEnd);

        var agg = responseAgg.GroupBy(w => w.ProjectId)
            .Select(g => new { ProjectId = g.Key, Words = g.Sum(x => x.WordsWritten) })
            .ToList();

        if (agg.Count == 0) 
            return new List<EventLeaderboardRowDto>();

        var projectIds = agg.Select(a => a.ProjectId).ToList();

        // infos de projeto/autor
        // var projInfo = await db.Projects
        //     .Where(p => projectIds.Contains(p.Id))
        //     .Select(p => new { p.Id, p.Title })
        //     .ToListAsync();

        var projInfo = await projectRepository.GetProjectById(projectIds.FirstOrDefault());

        // // vínculo no evento (meta/ganhou)
        // var peMap = await db.ProjectEvents
        //     .Where(pe => pe.EventId == eventId && projectIds.Contains(pe.ProjectId))
        //     .Select(pe => new { pe.ProjectId, pe.TargetWords, pe.ValidatedWords, pe.Won })
        //     .ToListAsync();
        
        
        var peMap = await projectEventsRepository.GetProjectEventByProjectIdAndEventId(projInfo.Id, ev.Id);

        var rows = agg.Select(a =>
            {
                var p = projInfo.Id;
                var pe = peMap.ProjectId;
            var target = (int)(peMap.TargetWords?? 0);
            var percent = target > 0 ? (double)a.Words / target * 100.0 : 0.0;

            return new EventLeaderboardRowDto
            {
                ProjectId = a.ProjectId,
                ProjectTitle = projInfo.Title ?? "Projeto",
                
                Words = a.Words,
                Percent = percent,
                Won = peMap.Won
            };
        })
        // regra de desempate: mais palavras, depois título
        .OrderByDescending(r => r.Words)
        .ThenBy(r => r.ProjectTitle)
        .ToList();

        // rank 1..N
        for (int i = 0; i < rows.Count; i++) rows[i].Rank = i + 1;

        var limit = Math.Clamp(top, 1, 200);
        return (List<EventLeaderboardRowDto>)rows.Take(limit);
    }
}