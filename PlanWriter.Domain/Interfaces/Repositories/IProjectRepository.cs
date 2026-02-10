using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Interfaces.Repositories
{
    public interface IProjectRepository
    {
        Task<Project> CreateAsync(Project project, CancellationToken ct);
        Task<IReadOnlyList<Project>> GetUserProjectsAsync(Guid userId, CancellationToken ct);
        Task<Project?> GetProjectWithProgressAsync(Guid id, Guid userId, CancellationToken ct);
        Task<bool> SetGoalAsync(Guid projectId, Guid userId, int wordCountGoal, DateTime? deadline = null, CancellationToken ct = default);
        Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, Guid userId);
        Task<bool> DeleteProjectAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task<Project?> GetProjectById(Guid id);
        Task ApplyValidationAsync(Guid projectId, ValidationResultDto res, CancellationToken ct);
        Task<(int? goalWords, string? title)> GetGoalAndTitleAsync(Guid projectId, CancellationToken ct);
        Task SaveValidationAsync(Guid projectId, int words, bool passed, DateTime utcNow, CancellationToken ct);
        Task<(int goalAmount, GoalUnit unit)> GetGoalAsync(Guid projectId, CancellationToken ct);
        Task<bool> UserOwnsProjectAsync(Guid projectId, Guid userId, CancellationToken ct);
        Task UpdateFlexibleGoalAsync(Guid projectId, int goalAmount, GoalUnit unit, DateTime? deadline, CancellationToken ct);
        Task<IReadOnlyList<ProjectDto>> GetByUserIdAsync(Guid userId, CancellationToken ct);
        Task<IReadOnlyList<Project>> GetPublicProjectsByUserIdAsync(Guid userId);
        Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default);
        Task UpdateAsync(Project project, CancellationToken ct);
    }
}
