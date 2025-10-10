using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.Application.Services;

public class ProjectStatsService(IProjectProgressRepository projectProgressRepository, IProjectRepository repo)
    : IProjectStatsService
{
    public async Task<ProjectGoalStatsDto> GetGoalStatsAsync(Guid projectId, CancellationToken ct)
    {
        var (goal, unit) = await repo.GetGoalAsync(projectId, ct);
        var acc = await projectProgressRepository.GetAccumulatedAsync(projectId, unit, ct);
        var remaining = Math.Max(0, goal - acc);
        return new ProjectGoalStatsDto(goal, unit, acc, remaining);
    }
}