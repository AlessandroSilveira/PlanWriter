using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories
{
    public interface IProjectRepository
    {
        Task<Project> CreateAsync(Project project);
        Task<IEnumerable<Project>> GetUserProjectsAsync(string userId);
        Task<Project> GetProjectWithProgressAsync(Guid id, string userId);
        Task<Project> GetUserProjectByIdAsync(Guid id, string userId);


    }
}