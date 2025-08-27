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
    IBadgeRepository badgeRepository
) : IBadgeServices
{
    public async Task<List<Badge>> CheckAndAssignBadgesAsync(Guid projectId, ClaimsPrincipal user)
    {
        var badges = new List<Badge>();
        var userId = userService.GetUserId(user);
        var project = await projectRepo.GetUserProjectByIdAsync(projectId, userId);

        if (project == null)
            return null;

        var entries = await progressRepo.GetProgressByProjectIdAsync(projectId, userId);
        var existingBadges = await badgeRepository.GetBadgesByProjectIdAsync(projectId);

        bool AlreadyHas(string badgeName) => existingBadges.Any(b => b.Name == badgeName);

        // ✍️ Primeiro Passo
        if (entries.Any() && !AlreadyHas("Primeiro Passo"))
        {
            badges.Add(new Badge
            {
                Name = "Primeiro Passo",
                Description = "Parabéns por registrar seu primeiro progresso!",
                Icon = "✍️",
                AwardedAt = DateTime.UtcNow,
                ProjectId = project.Id
            });
        }

        // 🔟 Dez Dias
        var uniqueDays = entries.Select(p => p.Date.Date).Distinct().ToList();
        if (uniqueDays.Count >= 10 && !AlreadyHas("Dez Dias"))
        {
            badges.Add(new Badge
            {
                Icon = "🔟",
                Name = "Dez Dias",
                AwardedAt = DateTime.UtcNow,
                ProjectId = project.Id,
                Description = "Parabéns por registrar seu progresso por dez dias diferentes!"
            });
        }

        // 💯 Cem Palavras
        if (entries.Any(p => p.WordsWritten > 100) && !AlreadyHas("Cem Palavras"))
        {
            badges.Add(new Badge
            {
                Icon = "💯",
                Name = "Cem Palavras",
                AwardedAt = DateTime.UtcNow,
                ProjectId = project.Id,
                Description = "Parabéns por escrever mais de 100 palavras em uma única entrada!"
            });
        }

        // 🧠 Constância (5 dias seguidos)
        var ordered = uniqueDays.OrderBy(d => d).ToList();
        int streak = 1;
        for (int i = 1; i < ordered.Count; i++)
        {
            if ((ordered[i] - ordered[i - 1]).Days == 1)
                streak++;
            else
                streak = 1;

            if (streak >= 5 && !AlreadyHas("Constância"))
            {
                badges.Add(new Badge
                {
                    Icon = "🧠",
                    Name = "Constância",
                    AwardedAt = DateTime.UtcNow,
                    ProjectId = project.Id,
                    Description = "Parabéns por escrever por 5 dias seguidos!"
                });
                break;
            }
        }

        // 🚀 Meta Atingida
        var totalWords = entries.Sum(p => p.WordsWritten);
        if ((project.WordCountGoal ?? 0) > 0 && totalWords >= project.WordCountGoal && !AlreadyHas("Meta Atingida"))
        {
            badges.Add(new Badge
            {
                Icon = "🚀",
                Name = "Meta Atingida",
                AwardedAt = DateTime.UtcNow,
                ProjectId = project.Id,
                Description = "Parabéns por atingir sua meta de palavras!"
            });
        }

        // Salvar se houver novidades
        if (badges.Count > 0)
            await badgeRepository.SaveBadges(badges);

        return badges;
    }

    public async Task<List<Badge>> GetBadgesByProjetcId(Guid projectId)
    {
        var badges = await badgeRepository.GetBadgesByProjectIdAsync(projectId);
        return badges.ToList();
    }
}