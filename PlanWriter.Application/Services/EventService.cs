using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;
using PlanWriter.Domain.Requests;
using CreateEventRequest = PlanWriter.Domain.Requests.CreateEventRequest;


namespace PlanWriter.Application.Services;

public class EventService
(
    IEventRepository             eventRepository,
    IProjectRepository           projectRepository,
    IProjectEventsRepository     projectEventsRepository,
    IProjectProgressRepository   projectProgressRepository,
    IBadgeRepository             badgeRepository
) : IEventService
{
    public async Task<EventDto[]> GetActiveAsync()
        => await eventRepository.GetActiveEvents();
    

    public async Task<EventDto?> GetByIdAsync(Guid eventId)
    {
        var ev = await eventRepository.GetEventById(eventId);
        return ev is null
            ? null
            : new EventDto(ev.Id, ev.Name, ev.Slug, ev.Type.ToString(),
                ev.StartsAtUtc, ev.EndsAtUtc, ev.DefaultTargetWords, ev.IsActive);
    }
    
    public async Task<List<EventDto>?> GetAllAsync()
    {
        var ev = await eventRepository.GetAllAsync();
        return ev is null
            ? null
            : ev;
    }

    public async Task UpdateAsync(Guid id, UpdateEventDto dto)
    {
        var ev = await eventRepository.GetEventById(id);

        if (ev is null) throw new InvalidOperationException("Evento não encontrado.");
        
        var type = Enum.TryParse<EventType>(dto.Type, true, out var t) ? t : EventType.Nanowrimo;
        
        ev.Name = dto.Name.Trim();
        ev.Slug = GenerateSlug(dto.Name);
        ev.Type = type;
        ev.StartsAtUtc = dto.StartDate;
        ev.EndsAtUtc   = dto.EndDate;
        ev.IsActive = dto.isActive;
        ev.DefaultTargetWords = dto.TargetWords;

        await eventRepository.UpdateAsync(ev, id);
        return;
    }

    public async Task DeleteAsync(Guid id)
    {
        var ev = await eventRepository.GetEventById(id);

        if (ev is null) throw new InvalidOperationException("Evento não encontrado.");

        await eventRepository.DeleteAsync(ev);
    }

    public async Task<List<MyEventDto>> GetMyEventsAsync(Guid userId)
    {
        var userEvents = await eventRepository.GetEventByUserId(userId);

        // calcula percent fora do LINQ (evita divisão no SQL)
        foreach (var item in userEvents)
        {
            item.Percent = item.TargetWords == 0
                ? 0
                : (int)Math.Round(
                    (decimal)item.TotalWrittenInEvent / (decimal)item.TargetWords * 100
                );
        }

        return userEvents;
    }



    public async Task<EventDto> CreateAsync(CreateEventRequest req)
    {
        var slugInUse = await eventRepository.GetEventBySlug(GenerateSlug(req.Name));
        if (slugInUse) throw new InvalidOperationException("Slug já está em uso.");
    
        var type = Enum.TryParse<EventType>(req.Type, true, out var t) ? t : EventType.Nanowrimo;
    
        var ev = new Event
        {
            Name = req.Name.Trim(),
            Slug = GenerateSlug(req.Name), 
            Type = type,
            StartsAtUtc = req.StartDate,
            EndsAtUtc   = req.EndDate,
            DefaultTargetWords = req.TargetWords,
            IsActive = true
        };
    
        await eventRepository.AddEvent(ev);
    
        return new EventDto(ev.Id, ev.Name, ev.Slug, ev.Type.ToString(),
            ev.StartsAtUtc, ev.EndsAtUtc, ev.DefaultTargetWords, ev.IsActive);
    }
    
    private static string GenerateSlug(string name)
    {
        return name
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "");
    }
    // ✅ atualizado: faz upsert de TargetWords se já estiver inscrito
    public async Task<ProjectEvent> JoinAsync(JoinEventRequest req)
    {
        var ev = await eventRepository.GetEventById(req.EventId)
                 ?? throw new KeyNotFoundException("Evento não encontrado.");

        var project = await projectRepository.GetProjectById(req.ProjectId)
                      ?? throw new KeyNotFoundException("Projeto não encontrado.");

        var existing = await projectEventsRepository
            .GetProjectEventByProjectIdAndEventId(req.ProjectId, req.EventId);

        if (existing != null)
        {
            if (req.TargetWords.HasValue && existing.TargetWords != req.TargetWords.Value)
            {
                existing.TargetWords = req.TargetWords.Value;
                await projectEventsRepository.UpdateProjectEvent(existing);
            }
            return existing;
        }

        var pe = new ProjectEvent
        {
            ProjectId   = req.ProjectId,
            EventId     = req.EventId,
            TargetWords = req.TargetWords ?? ev.DefaultTargetWords
        };

        return await projectEventsRepository.AddProjectEvent(pe);
    }

    public async Task<EventProgressDto> GetProgressAsync(Guid projectId, Guid eventId)
    {
        var pe = await projectEventsRepository
            .GetProjectEventByProjectIdAndEventId(projectId, eventId)
            ?? throw new KeyNotFoundException("Inscrição do projeto no evento não encontrada.");

        var ev = pe.Event!;
        var target = pe.TargetWords ?? ev.DefaultTargetWords ?? 50000;

        // soma de palavras dentro da janela do evento
        var entries = await projectProgressRepository.FindAsync(w =>
            w.ProjectId == projectId &&
            w.CreatedAt >= ev.StartsAtUtc &&
            w.CreatedAt <  ev.EndsAtUtc);

        var totalInEvent = entries.Sum(w => (int?)w.WordsWritten) ?? 0;

        var days = Math.Max(1, (int)Math.Ceiling((ev.EndsAtUtc - ev.StartsAtUtc).TotalDays));
        var dayIndex = Math.Clamp(
            (int)Math.Ceiling((DateTime.UtcNow - ev.StartsAtUtc).TotalDays),
            1, days);

        var dailyTarget = (int)Math.Ceiling((double)target / days);
        var percent = target > 0 ? (int)Math.Round(totalInEvent * 100.0 / target) : 0;
        var remaining = Math.Max(0, target - totalInEvent);

        return new EventProgressDto(
            projectId,
            eventId,
            target,
            totalInEvent,
            percent,
            remaining,
            days,
            dayIndex,
            dailyTarget,
            pe.Id,                 // ProjectEventId
            pe.ValidatedAtUtc,
            pe.Won,
            pe.Event.Name
        );
    }

    public async Task<ProjectEvent> FinalizeAsync(Guid projectEventId)
    {
        var pe = await projectEventsRepository.GetProjectEventByProjectId(projectEventId)
                 ?? throw new KeyNotFoundException("Inscrição não encontrada.");

        var ev = pe.Event ?? await eventRepository.GetEventById(pe.EventId)
                 ?? throw new KeyNotFoundException("Evento não encontrado.");

        var target = pe.TargetWords ?? ev.DefaultTargetWords ?? 50000;

        var entries = await projectProgressRepository.FindAsync(w =>
            w.ProjectId == pe.ProjectId &&
            w.CreatedAt >= ev.StartsAtUtc &&
            w.CreatedAt <  ev.EndsAtUtc);

        var totalInEvent = entries.Sum(w => (int?)w.WordsWritten) ?? 0;

        pe.FinalWordCount  = totalInEvent;
        pe.ValidatedWords  = totalInEvent;
        pe.ValidatedAtUtc  = DateTime.UtcNow;
        pe.Won             = totalInEvent >= target;

        await projectEventsRepository.UpdateProjectEvent(pe);

        // badge winner/participant do evento
        var badge = new Badge
        {
            ProjectId   = pe.ProjectId.Value,
            EventId     = ev.Id,
            Name        = (pe.Won ? "🏆 Winner — " : "🎉 Participant — ") + ev.Name,
            Description = pe.Won
                ? $"Você atingiu a meta de {target:N0} palavras no {ev.Name}!"
                : $"Obrigado por participar do {ev.Name}. Continue escrevendo!",
            Icon        = pe.Won ? "🏆" : "🎉",
            AwardedAt   = DateTime.UtcNow
        };
        await badgeRepository.SaveBadges(new List<Badge> { badge });

        return pe;
    }

    // public async Task<List<EventLeaderboardRowDto>> GetLeaberBoard(Guid eventId, string scope, int top)
    //      {
    //          var ev = await eventRepository.GetEventById(eventId)
    //              ?? throw new Exception("Evento nao encontrado.");
    //  
    //          var start = ev.StartsAtUtc.Date;
    //          var end = ev.EndsAtUtc.Date;
    //          var today = DateTime.UtcNow.Date;
    //          var effectiveEnd = today < end ? today : end;
    //  
    //          DateTime winStart = start, winEnd = effectiveEnd;
    //  
    //          var daily = string.Equals(scope, "daily", StringComparison.OrdinalIgnoreCase);
    //          if (daily)
    //          {
    //              if (today < start || today > end) return new List<EventLeaderboardRowDto>();
    //              winStart = winEnd = today;
    //          }
    //  
    //          var agg = (await projectProgressRepository.FindAsync(w =>
    //                  w.CreatedAt.Date >= winStart && w.CreatedAt.Date <= winEnd))
    //              .GroupBy(w => w.ProjectId)
    //              .Select(g => new { ProjectId = g.Key, Words = g.Sum(x => x.WordsWritten) })
    //              .ToList();
    //  
    //          if (agg.Count == 0) return new List<EventLeaderboardRowDto>();
    //  
    //          // Pegar metas por projeto no evento
    //          var metas = new Dictionary<Guid, int>();
    //          foreach (var row in agg)
    //          {
    //              var link = await projectEventsRepository.GetProjectEventByProjectIdAndEventId(row.ProjectId, eventId);
    //              var target = link?.TargetWords ?? ev.DefaultTargetWords ?? 50000;
    //              metas[row.ProjectId] = target;
    //          }
    //  
    //          var rows = agg
    //              .Select((a, _) => new EventLeaderboardRowDto
    //              {
    //                  ProjectId    = a.ProjectId,
    //                  ProjectTitle = "",   // título pode ser preenchido se necessário (não impacta ordenação)
    //                  UserName     = "",
    //                  Words        = a.Words,
    //                  Percent      = metas.TryGetValue(a.ProjectId, out var tgt) && tgt > 0
    //                                  ? (double)a.Words / tgt * 100.0
    //                                  : 0.0,
    //                  Won          = false,
    //                  Rank         = 0
    //              })
    //              .OrderByDescending(r => r.Words)
    //              .ThenBy(r => r.ProjectTitle)
    //              .ToList();
    //  
    //          for (int i = 0; i < rows.Count; i++) rows[i].Rank = i + 1;
    //  
    //          var limit = Math.Clamp(top, 1, 200);
    //          return rows.Take(limit).ToList();
    //      }
    
    
    public async Task<List<EventLeaderboardRowDto>> GetLeaberBoard(Guid eventId, string scope, int top)
    {
        var ev = await eventRepository.GetEventById(eventId)
            ?? throw new Exception("Evento não encontrado.");

        var start = ev.StartsAtUtc.Date;
        var end = ev.EndsAtUtc.Date;
        var today = DateTime.UtcNow.Date;
        var effectiveEnd = today < end ? today : end;

        var winStart = start;
        var winEnd = effectiveEnd;

        var daily = string.Equals(scope, "daily", StringComparison.OrdinalIgnoreCase);
        if (daily)
        {
            if (today < start || today > end)
                return new List<EventLeaderboardRowDto>();

            winStart = winEnd = today;
        }
        var query = await eventRepository.GetLeaderboard(ev, winStart, winEnd, top);

        var rows =  query
            .OrderByDescending(r => r.Words)
            .ThenBy(r => r.ProjectTitle)
            .Take(Math.Clamp(top, 1, 200))
            .ToList();

        for (int i = 0; i < rows.Count; i++)
            rows[i].Rank = i + 1;

        return rows;
    }


    // ✅ novo
    public async Task LeaveAsync(Guid projectId, Guid eventId)
    {
        await projectEventsRepository.RemoveByKeys(projectId, eventId);
    }
    
    
}
