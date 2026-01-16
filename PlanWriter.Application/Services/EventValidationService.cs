// Application/Events/EventValidationService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Services;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Application.Services;

public class EventValidationService : IEventValidationService
{
    private readonly AppDbContext _db;
    public EventValidationService(AppDbContext db) => _db = db;

    private async Task<(Event ev, ProjectEvent pe)> LoadAsync(Guid userId, Guid eventId, Guid projectId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId)
                 ?? throw new KeyNotFoundException("Evento não encontrado.");
        var proj = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId)
                   ?? throw new InvalidOperationException("Projeto não encontrado ou não pertence ao usuário.");
        var pe = await _db.ProjectEvents.FirstOrDefaultAsync(x => x.EventId == eventId && x.ProjectId == projectId)
                 ?? throw new InvalidOperationException("Projeto não está inscrito neste evento.");
        return (ev, pe);
    }

    public async Task<(int target, int total)> PreviewAsync(Guid userId, Guid eventId, Guid projectId)
    {
        var (ev, pe) = await LoadAsync(userId, eventId, projectId);
        var target = pe.TargetWords ?? ev.DefaultTargetWords ?? 50000;

        var total = await _db.Set<ProjectProgress>()
            .Where(w => w.ProjectId == projectId
                        && w.CreatedAt >= ev.StartsAtUtc
                        && w.CreatedAt <  ev.EndsAtUtc)
            .SumAsync(w => (int?)w.WordsWritten) ?? 0;

        return (target, total);
    }

    public async Task ValidateAsync(Guid userId, Guid eventId, Guid projectId, int words, string source)
    {
        var (ev, pe) = await LoadAsync(userId, eventId, projectId);

        var target = pe.TargetWords ?? ev.DefaultTargetWords ?? 50000;
        if (words < target)
            throw new InvalidOperationException($"Total informado ({words}) é menor que a meta ({target}).");

        pe.Won = true;
        pe.ValidatedAtUtc = DateTime.UtcNow;
        pe.ValidatedWords = words;
        pe.ValidationSource = string.IsNullOrWhiteSpace(source) ? "manual" : source;

        await _db.SaveChangesAsync();
    }
}