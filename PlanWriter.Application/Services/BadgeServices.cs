using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.Application.Services;

public class BadgeServices(
    IProjectRepository projectRepo,
    IProjectProgressRepository progressRepo,
    IUserService userService,
    IBadgeRepository badgeRepository)
    : IBadgeServices
{
    /// <summary>
    /// Entry point seguro para ser chamado após AddProgress
    /// </summary>
    public async Task GrantAsync(Guid projectId, ClaimsPrincipal user)
    {
        try
        {
            await CheckAndAssignBadgesAsync(projectId, user);
        }
        catch (Exception ex)
        {
            // ⚠️ NUNCA quebrar escrita por causa de badge
            Console.WriteLine($"[BadgeServices] Erro ao avaliar badges: {ex}");
        }
    }

    public async Task<List<Badge>> CheckAndAssignBadgesAsync(Guid projectId, ClaimsPrincipal user)
    {
        var userId = userService.GetUserId(user);
        var project = await projectRepo.GetUserProjectByIdAsync(projectId, userId);
        if (project == null) return new();

        var entries = await progressRepo.GetProgressByProjectIdAsync(projectId, userId);
        var projectProgresses = entries.ToList();
        if (entries == null || projectProgresses.Count == 0) return new();

        var existing = (await badgeRepository.GetBadgesByProjectIdAsync(projectId))
            .Select(b => b.Name)
            .ToHashSet();

        var newBadges = new List<Badge>();

        void AddIfMissing(string name, string desc, string icon)
        {
            if (existing.Contains(name)) return;

            newBadges.Add(new Badge
            {
                
                ProjectId = projectId,
                Name = name,
                Description = desc,
                Icon = icon,
                AwardedAt = DateTime.UtcNow
            });
        }

        // ✍️ Primeiro passo
        AddIfMissing(
            "Primeiro Passo",
            "Parabéns por registrar seu primeiro progresso!",
            "✍️"
        );

        // 💯 100 palavras em um dia
        if (projectProgresses.Any(e => e.WordsWritten >= 100))
        {
            AddIfMissing(
                "Cem Palavras",
                "Parabéns por escrever 100 palavras em uma única sessão!",
                "💯"
            );
        }

        // 🔟 10 dias distintos
        var days = projectProgresses.Select(e => e.Date.Date).Distinct().Count();
        if (days >= 10)
        {
            AddIfMissing(
                "Dez Dias",
                "Você escreveu em 10 dias diferentes!",
                "🔟"
            );
        }

        // 🧠 Streak real (dias consecutivos)
        var daySet = projectProgresses
            .Select(e => e.Date.Date)
            .Distinct()
            .ToHashSet();

        int streak = 0;
        for (var d = DateTime.UtcNow.Date; daySet.Contains(d); d = d.AddDays(-1))
            streak++;

        if (streak >= 5)
            AddIfMissing("Constância", "5 dias seguidos escrevendo!", "🧠");
        if (streak >= 7)
            AddIfMissing("Streak 7 Dias", "Uma semana inteira escrevendo!", "🔥");
        if (streak >= 14)
            AddIfMissing("Streak 14 Dias", "Duas semanas de constância!", "⚡");
        if (streak >= 30)
            AddIfMissing("Streak 30 Dias", "Um mês sem falhar!", "🏅");

        // 🚀 Meta atingida
        if (project.WordCountGoal.HasValue &&
            project.CurrentWordCount >= project.WordCountGoal.Value)
        {
            AddIfMissing(
                "Meta Atingida",
                "Você alcançou a meta do projeto!",
                "🚀"
            );
        }

        if (newBadges.Count > 0)
            await badgeRepository.SaveBadges(newBadges);

        return newBadges;
    }

    public async Task<List<Badge>> GetBadgesByProjetcId(Guid projectId) 
        => (await badgeRepository.GetBadgesByProjectIdAsync(projectId)).ToList();
}
