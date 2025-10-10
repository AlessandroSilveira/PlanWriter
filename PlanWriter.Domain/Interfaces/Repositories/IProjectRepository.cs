using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Interfaces.Repositories
{
    public interface IProjectRepository: IRepository<Project>
    {
        Task<Project> CreateAsync(Project project);
        Task<IEnumerable<Project>> GetUserProjectsAsync(string userId);
        Task<Project> GetProjectWithProgressAsync(Guid id, string userId);
        Task<Project?> GetUserProjectByIdAsync(Guid id, string userId);
        Task<bool> SetGoalAsync(Guid projectId, string userId, int wordCountGoal, DateTime? deadline = null);
        Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, string userId);
        Task<bool> DeleteProjectAsync(Guid projectId, string userId);
        Task<Project?> GetProjectById(Guid id);
        Task ApplyValidationAsync(Guid projectId, ValidationResultDto res, CancellationToken ct);
        Task<(int? goalWords, string? title)> GetGoalAndTitleAsync(Guid projectId, CancellationToken ct);
        Task SaveValidationAsync(Guid projectId, int words, bool passed, DateTime utcNow, CancellationToken ct);
        Task<(int goalAmount, GoalUnit unit)> GetGoalAsync(Guid projectId, CancellationToken ct);
        Task<bool> UserOwnsProjectAsync(Guid projectId, Guid userId, CancellationToken ct);
        Task UpdateFlexibleGoalAsync(Guid projectId, int goalAmount, GoalUnit unit, DateTime? deadline, CancellationToken ct);

        
    }
        

    
}