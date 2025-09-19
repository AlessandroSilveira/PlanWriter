using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Application.Common;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Services;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Application.Services;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db) => _db = db;

    public async Task<MyProfileDto> GetMineAsync(Guid userId)
    {
        var me = await _db.Set<User>().FirstAsync(u => u.Id == userId);
        var publicIds = await _db.Set<Project>()
            .Where(p => p.UserId == userId.ToString() && p.IsPublic)
            .Select(p => p.Id)
            .ToArrayAsync();

        return new MyProfileDto(
            Email: me.Email!,
            DisplayName: me.DisplayName,
            Bio: me.Bio,
            AvatarUrl: me.AvatarUrl,
            IsProfilePublic: me.IsProfilePublic,
            Slug: me.Slug,
            PublicProjectIds: publicIds
        );
    }

    public async Task<MyProfileDto> UpdateMineAsync(Guid userId, UpdateMyProfileRequest req)
    {
        var me = await _db.Set<User>().FirstAsync(u => u.Id == userId);

        if (req.DisplayName != null) me.DisplayName = req.DisplayName.Trim();
        if (req.Bio != null) me.Bio = req.Bio?.Trim();
        if (req.AvatarUrl != null) me.AvatarUrl = string.IsNullOrWhiteSpace(req.AvatarUrl) ? null : req.AvatarUrl.Trim();
        if (req.IsProfilePublic.HasValue) me.IsProfilePublic = req.IsProfilePublic.Value;

        if (req.Slug != null)
        {
            var slug = Slugify.From(req.Slug);
            if (string.IsNullOrWhiteSpace(slug))
                throw new InvalidOperationException("Slug inválido.");

            var exists = await _db.Set<User>()
                .AnyAsync(u => u.Id != userId && u.Slug == slug);
            if (exists) throw new InvalidOperationException("Este slug já está em uso.");
            me.Slug = slug;
        }
        else if (string.IsNullOrWhiteSpace(me.Slug) && !string.IsNullOrWhiteSpace(me.DisplayName))
        {
            // gera um slug padrão baseado no DisplayName
            var baseSlug = Slugify.From(me.DisplayName);
            var slug = baseSlug;
            int i = 2;
            while (await _db.Set<User>().AnyAsync(u => u.Slug == slug))
                slug = $"{baseSlug}-{i++}";
            me.Slug = slug;
        }

        if (req.PublicProjectIds != null)
        {
            var ids = req.PublicProjectIds.Distinct().ToHashSet();
            var myProjects = await _db.Set<Project>().Where(p => p.UserId == userId.ToString()).ToListAsync();
            foreach (var p in myProjects)
                p.IsPublic = ids.Contains(p.Id);
        }

        await _db.SaveChangesAsync();
        return await GetMineAsync(userId);
    }

    public async Task<PublicProfileDto> GetPublicAsync(string slug)
    {
        var u = await _db.Set<User>().FirstOrDefaultAsync(x => x.Slug == slug)
                ?? throw new KeyNotFoundException("Perfil não encontrado.");
        if (!u.IsProfilePublic) throw new InvalidOperationException("Perfil não é público.");

        // Evento ativo global (se houver)
        var now = DateTime.UtcNow;
        var activeEvent = await _db.Events
            .FirstOrDefaultAsync(e => e.IsActive && e.StartsAtUtc <= now && e.EndsAtUtc >= now);

        // projetos públicos do usuário
        var projects = await _db.Set<Project>()
            .Where(p => p.UserId == u.Id.ToString() && p.IsPublic)
            .Select(p => new { p.Id, p.Title, p.WordCountGoal, p.CurrentWordCount })
            .ToListAsync();

        var summaries = new List<PublicProjectSummaryDto>();
        foreach (var p in projects)
        {
            int? pct = null; int? total = null; int? target = null; string? evName = null;

            if (activeEvent != null)
            {
                var pe = await _db.ProjectEvents
                    .FirstOrDefaultAsync(x => x.ProjectId == p.Id && x.EventId == activeEvent.Id);

                if (pe != null)
                {
                    target = pe.TargetWords ?? activeEvent.DefaultTargetWords ?? 50000;
                    total = await _db.Set<ProjectProgress>()
                        .Where(w => w.ProjectId == p.Id
                                    && w.CreatedAt >= activeEvent.StartsAtUtc
                                    && w.CreatedAt <  activeEvent.EndsAtUtc)
                        .SumAsync(w => (int?)w.WordsWritten) ?? 0;
                    pct = (int)Math.Min(100, Math.Round((decimal)(100.0 * total / Math.Max(1, target.Value))));
                    evName = activeEvent.Name;
                }
            }

            summaries.Add(new PublicProjectSummaryDto(
                ProjectId: p.Id,
                Title: p.Title ?? "Projeto",
                CurrentWords: p.CurrentWordCount,
                WordGoal: p.WordCountGoal,
                EventPercent: pct,
                EventTotalWritten: total,
                EventTargetWords: target,
                ActiveEventName: evName
            ));
        }

        // Destaque simples: se venceu algum evento recente, mostre
        string? highlight = null;
        var recentWin = await _db.ProjectEvents
            .Include(pe => pe.Event)
            .Where(pe => pe.Project!.UserId == u.Id.ToString() && pe.Won && pe.ValidatedAtUtc != null)
            .OrderByDescending(pe => pe.ValidatedAtUtc)
            .FirstOrDefaultAsync();
        if (recentWin != null)
            highlight = $"Winner — {recentWin.Event!.Name}";

        return new PublicProfileDto(
            DisplayName: u.DisplayName ?? u.Email ?? "Autor(a)",
            Bio: u.Bio,
            AvatarUrl: u.AvatarUrl,
            Slug: u.Slug!,
            Projects: summaries.ToArray(),
            Highlight: highlight
        );
    }
}