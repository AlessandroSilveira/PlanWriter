using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.ReadModels;

public interface IProjectProgressReadRepository
{
    Task<int> GetMonthlyWordsAsync(Guid userId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
    Task<int> GetMonthlyWordsAsync(Guid userId, DateTime start, DateTime end);
    Task<IEnumerable<ProjectProgressDayDto>> GetProgressByDayAsync(Guid projectId, Guid userId);
    Task<ProgressRow?> GetByIdAsync(Guid progressId, Guid userId, CancellationToken ct);
    Task<int> GetLastTotalBeforeAsync(Guid projectId, Guid userId, DateTime date, CancellationToken ct);
    Task<IEnumerable<ProgressHistoryRow>> GetProgressHistoryAsync(Guid projectId, Guid userId, CancellationToken ct);
   
}

