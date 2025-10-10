using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectProgressRepository : IRepository<ProjectProgress>
{
    Task<IEnumerable<ProjectProgress>> GetProgressByProjectIdAsync(Guid projectId, string userId);
    Task<ProjectProgress> AddProgressAsync(ProjectProgress progress);
    Task<IEnumerable<ProjectProgress>> GetProgressHistoryAsync(Guid projectId, string userId);
    Task<ProjectProgress> GetLastProgressBeforeAsync(Guid projectId, DateTime date);
    Task<bool> DeleteAsync(Guid id, string userId);
    Task<ProjectProgress> GetByIdAsync(Guid id, string userId);
    Task<int> GetAccumulatedAsync(Guid projectId, GoalUnit unit, CancellationToken ct);
}