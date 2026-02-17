using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Interfaces.ReadModels.Projects;

public interface IProjectProgressReadRepository
{
    Task<int> GetMonthlyWordsAsync(Guid userId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
   
    Task<IReadOnlyList<ProjectProgressDayDto>> GetProgressByDayAsync(Guid projectId, Guid userId, CancellationToken ct);
    Task<ProgressRow?> GetByIdAsync(Guid progressId, Guid userId, CancellationToken ct);
    Task<int> GetLastTotalBeforeAsync(Guid projectId, Guid userId, DateTime date, CancellationToken ct);
    Task<IReadOnlyList<ProgressHistoryRow>> GetProgressHistoryAsync(Guid projectId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<ProgressHistoryRow>> GetUserProgressByDayAsync(Guid userId, DateTime startDate, DateTime endDate, Guid? projectId, CancellationToken ct);
    Task<IReadOnlyList<ProjectProgress>> GetProgressByProjectIdAsync(Guid projectId, Guid userId, CancellationToken ct);
    
    Task<List<ProjectProgress>> GetProgressHistoryAsync(Guid projectId, Guid userId);
    //Task<ProjectProgress> GetLastProgressBeforeAsync(Guid projectId, DateTime date);
    
    //Task<ProjectProgress> GetByIdAsync(Guid id, Guid userId);
    Task<int> GetAccumulatedAsync(Guid projectId, GoalUnit unit, CancellationToken ct);
    Task<Dictionary<Guid, int>> GetTotalWordsByUsersAsync(IEnumerable<Guid> userIds, DateTime? start, DateTime? end);
    Task<int> GetMonthlyWordsAsync(Guid userId, DateTime start, DateTime end);
   
}
