using System;
using PlanWriter.Domain.Entities;
using System.Threading.Tasks;

namespace PlanWriter.Domain.Interfaces
{
    public interface IProjectRepository
    {
        Task AddAsync(Project project);
        Task<Project?> GetByIdAsync(Guid id);
        Task UpdateAsync(Project project);
        Task AddProgressEntryAsync(ProjectProgressEntry entry);

    }
}