using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IDailyWordLogRepository
{
    Task<DailyWordLog?> GetByProjectAndDateAsync(Guid projectId, DateOnly date, Guid userId);

    Task<IEnumerable<DailyWordLog>> GetByProjectAsync(Guid projectId, Guid userId);

    Task AddAsync(DailyWordLog log);

    Task UpdateAsync(DailyWordLog log);
    Task<int> SumWordsAsync(Guid userId, DateTime? start, DateTime? end);
    Task<Dictionary<Guid, int>> GetTotalWordsByUsersAsync(IEnumerable<Guid> userIds, DateOnly? start, DateOnly? end);
}