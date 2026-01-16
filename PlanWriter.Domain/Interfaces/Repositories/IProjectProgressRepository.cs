using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectProgressRepository : IRepository<ProjectProgress>
{
    Task<IEnumerable<ProjectProgress>> GetProgressByProjectIdAsync(Guid projectId, Guid userId);
    Task<ProjectProgress> AddProgressAsync(ProjectProgress progress);
    Task<IEnumerable<ProjectProgress>> GetProgressHistoryAsync(Guid projectId, Guid userId);
    Task<ProjectProgress> GetLastProgressBeforeAsync(Guid projectId, DateTime date);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<ProjectProgress> GetByIdAsync(Guid id, Guid userId);
    Task<int> GetAccumulatedAsync(Guid projectId, GoalUnit unit, CancellationToken ct);
    Task<Dictionary<Guid, int>> GetTotalWordsByUsersAsync(IEnumerable<Guid> userIds, DateTime? start, DateTime? end);
    Task<int> GetMonthlyWordsAsync(Guid userId, DateTime start, DateTime end);
}