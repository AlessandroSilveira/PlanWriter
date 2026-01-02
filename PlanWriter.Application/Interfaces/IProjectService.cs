using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Enums;



namespace PlanWriter.Application.Interfaces
{
    public interface IProjectService
    {
        Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, ClaimsPrincipal user);
        Task<IEnumerable<ProjectDto>> GetUserProjectsAsync(ClaimsPrincipal user);
        Task<ProjectDto> GetProjectByIdAsync(Guid id, ClaimsPrincipal user);
        Task AddProgressAsync(AddProjectProgressDto dto, ClaimsPrincipal user);
        Task<IEnumerable<ProgressHistoryDto>> GetProgressHistoryAsync(Guid projectId, ClaimsPrincipal user);
        Task<bool> SetGoalAsync(Guid projectId, string userId, int wordCountGoal, DateTime? deadline);
        Task<bool> DeleteProjectAsync(Guid projectId, string userId);
        Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, string userId);
        Task<bool> DeleteProgressAsync(Guid progressId, string userId);
        Task<ProjectStatsDto> GetStatsAsync(Guid projectId, ClaimsPrincipal user);
        Task UploadCoverAsync(Guid projectId, ClaimsPrincipal user, byte[] bytes, string mime, int size);
        Task DeleteCoverAsync(Guid projectId, ClaimsPrincipal user);
        Task<(byte[] bytes, string mime, int size, DateTime? updatedAt)?> GetCoverAsync(Guid projectId, ClaimsPrincipal user);
        Task SetFlexibleGoalAsync(Guid projectId, Guid userId, int goalAmount, GoalUnit goalUnit, DateTime? deadline, CancellationToken ct = default);


        Task CreateFromSprintAsync(CreateSprintProgressDto dto, CancellationToken ct);
    }
}