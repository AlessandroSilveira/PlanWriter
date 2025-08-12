using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IProjectService
{
    Task CreateProjectAsync(CreateProjectDto dto, ClaimsPrincipal user);
    Task<IEnumerable<ProjectDto>> GetUserProjectsAsync(ClaimsPrincipal user);
    Task<ProjectDto> GetProjectByIdAsync(Guid id, ClaimsPrincipal user);
    Task AddProgressAsync(AddProjectProgressDto dto, ClaimsPrincipal user);
    Task<IEnumerable<ProgressHistoryDto>> GetProgressHistoryAsync(Guid projectId, ClaimsPrincipal user);
    Task SetGoalAsync(Guid projectId, SetGoalDto dto, ClaimsPrincipal user);
    Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, ClaimsPrincipal user);
    Task DeleteProjectAsync(Guid id, ClaimsPrincipal user);
}