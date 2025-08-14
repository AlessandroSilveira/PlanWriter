using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories
{
    public interface IProjectRepository
    {
        Task<Project> CreateAsync(Project project);
        Task<IEnumerable<Project>> GetUserProjectsAsync(string userId);
        Task<Project> GetProjectWithProgressAsync(Guid id, string userId);
        Task<Project> GetUserProjectByIdAsync(Guid id, string userId);
        Task<bool> SetGoalAsync(Guid projectId, string userId, int wordCountGoal, DateTime? deadline = null);
        Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, string userId);
        Task<bool> DeleteProjectAsync(Guid projectId, string userId);

    }
}