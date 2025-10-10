using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IProjectStatsService
{
    Task<ProjectGoalStatsDto> GetGoalStatsAsync(Guid projectId, CancellationToken ct);
}