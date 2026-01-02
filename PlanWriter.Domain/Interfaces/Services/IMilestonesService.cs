using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IMilestonesService
{
    Task<IReadOnlyList<MilestoneDto>> GetProjectMilestonesAsync(Guid projectId, ClaimsPrincipal user, CancellationToken ct);
    Task<MilestoneDto> CreateAsync(Guid projectId, ClaimsPrincipal user, CreateMilestoneDto dto, CancellationToken ct);
    Task DeleteAsync(Guid milestoneId, ClaimsPrincipal user, CancellationToken ct);
    Task EvaluateAutoMilestonesAsync(Guid projectId, int totalAccum, CancellationToken ct);
    Task EvaluateMilestonesAsync(Guid projectId, int totalAccum, CancellationToken ct);

}