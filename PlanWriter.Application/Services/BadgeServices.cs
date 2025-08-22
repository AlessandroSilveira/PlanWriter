using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.Application.Services;

public class BadgeServices : IBadgeServices
{
    public async Task<List<Badge>> CheckAndAssignBadgesAsync(Project project)
    {
        var badges = new List<Badge>();

        var hasFirstProgress = await _dbContext.ProgressEntries
            .AnyAsync(p => p.ProjectId == project.Id);

        if (hasFirstProgress && !_dbContext.Badges.Any(b => b.ProjectId == project.Id && b.Name == "Primeiro Passo"))
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

        if (badges.Any())
        {
            await _dbContext.Badges.AddRangeAsync(badges);
            await _dbContext.SaveChangesAsync();
        }

        return badges;
    }
}