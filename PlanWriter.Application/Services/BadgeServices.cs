using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore; // garanta este using no topo

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
        var streak = 1;
        for (var i = 1; i < ordered.Count; i++)
        {
            if ((ordered[i] - ordered[i - 1]).Days == 1)
                streak++;
            else
                streak = 1;

            if (streak < 5 || AlreadyHas("Constância")) 
                continue;
            
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
        
        // === Streak 7/14/30/100 (dias consecutivos deste projeto) ===
        // Ajuste o campo de data conforme seu modelo: CreatedAt ou CreatedAtUtc
        var hoje = DateTime.UtcNow.Date;
        var diasComEscrita = await progressRepo.FindAsync(a=>a.ProjectId == project.Id);
            
         var diasComEscritas = diasComEscrita.GroupBy(w => w.CreatedAt.Date) // <- troque para w.CreatedAt.Date se for o seu campo
            .Select(g => g.Key)
            .ToList();

        var set = new HashSet<DateTime>(diasComEscritas);
         var streak2 = 0;
        for (var d = hoje; set.Contains(d); d = d.AddDays(-1)) streak++;

// Award só se ainda não possui (mesmo padrão dos seus outros badges)
        if (streak2 >= 7)   await AwardIfMissingAsync(projectId, "Streak 7 Dias",   "Uma semana inteira escrevendo!", "🔥", badges);
        if (streak2 >= 14)  await AwardIfMissingAsync(projectId, "Streak 14 Dias",  "Duas semanas de constância!",    "⚡", badges);
        if (streak2 >= 30)  await AwardIfMissingAsync(projectId, "Streak 30 Dias",  "Um mês sem falhar!",             "🏅", badges);
        if (streak2 >= 100) await AwardIfMissingAsync(projectId, "Streak 100 Dias", "Trilha lendária!",               "🏆", badges);


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

    private async Task AwardIfMissingAsync(Guid projectId, string name, string description, string icon,
        List<Badge> badges)
    {
        var exists = await badgeRepository.GetBadgesByProjectIdAsync(projectId);

        if (!exists.Any(a => a.Name == name))
        {
            badges.Add(new Badge
            {
                ProjectId  = projectId,
                Name       = name,
                Description= description,
                Icon       = icon,
                AwardedAt  = DateTime.UtcNow
            });

        }
    }
}