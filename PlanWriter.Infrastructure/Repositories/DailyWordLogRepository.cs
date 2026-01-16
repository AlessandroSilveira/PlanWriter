// Infrastructure/Repositories/DailyWordLogRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;


namespace PlanWriter.Infrastructure.Repositories;

public class DailyWordLogRepository(AppDbContext context) : IDailyWordLogRepository
{
    public async Task<DailyWordLog?> GetByProjectAndDateAsync(
        Guid projectId,
        DateOnly date,
        Guid userId
    )
    {
        return await context.Set<DailyWordLog>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userId &&
                x.Date == date
            );
    }

    public async Task<IEnumerable<DailyWordLog>> GetByProjectAsync(
        Guid projectId,
        Guid userId
    )
    {
        return await context.Set<DailyWordLog>()
            .AsNoTracking()
            .Where(x =>
                x.ProjectId == projectId &&
                x.UserId == userId
            )
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task AddAsync(DailyWordLog log)
    {
        context.Set<DailyWordLog>().Add(log);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DailyWordLog log)
    {
        context.Set<DailyWordLog>().Update(log);
        await context.SaveChangesAsync();
    }
    
    public async Task<int> SumWordsAsync(Guid userId, DateTime? start, DateTime? end)
    {
        var query = context.DailyWordLogs.Where(x => x.UserId == userId);

        if (start.HasValue)
        {
            var startDate = DateOnly.FromDateTime(start.Value);
            query = query.Where(x => x.Date >= startDate);
        }

        if (end.HasValue)
        {
            var endDate = DateOnly.FromDateTime(end.Value);
            query = query.Where(x => x.Date <= endDate);
        }

        return await query.SumAsync(x => (int?)x.WordsWritten) ?? 0;
    }

    public async Task<Dictionary<Guid, int>> GetTotalWordsByUsersAsync(IEnumerable<Guid> userIds, DateOnly? start, DateOnly? end)
    {
        var query = context.DailyWordLogs
            .Where(l => userIds.Contains(l.UserId));

        if (start.HasValue)
            query = query.Where(l => l.Date >= start.Value);

        if (end.HasValue)
            query = query.Where(l => l.Date <= end.Value);

        return await query
            .GroupBy(l => l.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Total = g.Sum(x => x.WordsWritten)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.Total);
    }
}