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

        var hasFirstProgress = entries.Any();

        if (hasFirstProgress && ! await badgeRepository.HasFirstStepsBadge(projectId))
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

        // Outros critérios como activeDays, meta atingida, etc podem vir aqui

        if (badges.Count != 0)
            await badgeRepository.SaveBadges(badges);

        return badges;
    }
}