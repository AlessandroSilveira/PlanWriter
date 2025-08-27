using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IBadgeServices
{
    Task<List<Badge>> CheckAndAssignBadgesAsync(Guid projectId, ClaimsPrincipal user);
    Task<List<Badge>> GetBadgesByProjetcId(Guid projectId);
}