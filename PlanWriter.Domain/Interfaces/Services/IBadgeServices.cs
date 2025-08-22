using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IBadgeServices
{
    Task<List<Badge>> CheckAndAssignBadgesAsync(Project project);
}