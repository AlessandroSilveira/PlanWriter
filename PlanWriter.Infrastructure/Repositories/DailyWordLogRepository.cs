// Infrastructure/Repositories/DailyWordLogRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;


namespace PlanWriter.Infrastructure.Repositories;

public class DailyWordLogRepository(IDbExecutor db) : IDailyWordLogRepository
{
    public Task<DailyWordLog?> GetByProjectAndDateAsync(
        Guid projectId,
        DateOnly date,
        Guid userId
    )
    {
        const string sql = @"
            SELECT TOP 1
                Id,
                ProjectId,
                UserId,
                [Date],
                WordsWritten,
                CreatedAtUtc
            FROM DailyWordLogs
            WHERE ProjectId = @ProjectId
              AND UserId = @UserId
              AND [Date] = @Date;
        ";

        return db.QueryFirstOrDefaultAsync<DailyWordLog>(
            sql,
            new { ProjectId = projectId, UserId = userId, Date = date }
        );
    }

    public async Task<IEnumerable<DailyWordLog>> GetByProjectAsync(
        Guid projectId,
        Guid userId
    )
    {
        const string sql = @"
            SELECT
                Id,
                ProjectId,
                UserId,
                [Date],
                WordsWritten,
                CreatedAtUtc
            FROM DailyWordLogs
            WHERE ProjectId = @ProjectId
              AND UserId = @UserId
            ORDER BY [Date];
        ";

        var rows = await db.QueryAsync<DailyWordLog>(sql, new { ProjectId = projectId, UserId = userId });
        return rows;
    }

    public Task AddAsync(DailyWordLog log)
    {
        const string sql = @"
            INSERT INTO DailyWordLogs
            (
                Id,
                ProjectId,
                UserId,
                [Date],
                WordsWritten,
                CreatedAtUtc
            )
            VALUES
            (
                @Id,
                @ProjectId,
                @UserId,
                @Date,
                @WordsWritten,
                @CreatedAtUtc
            );
        ";

        if (log.Id == Guid.Empty)
            log.Id = Guid.NewGuid();

        return db.ExecuteAsync(sql, new
        {
            log.Id,
            log.ProjectId,
            log.UserId,
            log.Date,
            log.WordsWritten,
            log.CreatedAtUtc
        });
    }

    public Task UpdateAsync(DailyWordLog log)
    {
        const string sql = @"
            UPDATE DailyWordLogs
            SET
                WordsWritten = @WordsWritten
            WHERE Id = @Id;
        ";

        return db.ExecuteAsync(sql, new { log.Id, log.WordsWritten });
    }
    
    public async Task<int> SumWordsAsync(Guid userId, DateTime? start, DateTime? end)
    {
        var startDate = start.HasValue ? DateOnly.FromDateTime(start.Value) : (DateOnly?)null;
        var endDate = end.HasValue ? DateOnly.FromDateTime(end.Value) : (DateOnly?)null;

        const string sql = @"
            SELECT COALESCE(SUM(WordsWritten), 0)
            FROM DailyWordLogs
            WHERE UserId = @UserId
              AND (@Start IS NULL OR [Date] >= @Start)
              AND (@End IS NULL OR [Date] <= @End);
        ";

        return await db.QueryFirstOrDefaultAsync<int>(
            sql,
            new { UserId = userId, Start = startDate, End = endDate }
        );
    }

    public async Task<Dictionary<Guid, int>> GetTotalWordsByUsersAsync(IEnumerable<Guid> userIds, DateOnly? start, DateOnly? end)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, int>();

        const string sql = @"
            SELECT
                UserId,
                SUM(WordsWritten) AS Total
            FROM DailyWordLogs
            WHERE UserId IN @UserIds
              AND (@Start IS NULL OR [Date] >= @Start)
              AND (@End IS NULL OR [Date] <= @End)
            GROUP BY UserId;
        ";

        var rows = await db.QueryAsync<(Guid UserId, int Total)>(
            sql,
            new { UserIds = ids, Start = start, End = end }
        );

        return rows.ToDictionary(x => x.UserId, x => x.Total);
    }
}
