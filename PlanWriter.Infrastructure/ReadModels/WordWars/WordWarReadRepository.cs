using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.WordWars;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.WordWars;

public class WordWarReadRepository(IDbExecutor db) : IWordWarReadRepository
{
    public Task<EventWordWarsDto?> GetByIdAsync(Guid warId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT TOP 1
                Id,
                EventId,
                CreatedByUserId,
                CASE
                    WHEN TRY_CONVERT(INT, Status) IS NOT NULL THEN TRY_CONVERT(INT, Status)
                    WHEN UPPER(LTRIM(RTRIM(CONVERT(NVARCHAR(20), Status)))) = 'WAITING' THEN 0
                    WHEN UPPER(LTRIM(RTRIM(CONVERT(NVARCHAR(20), Status)))) = 'RUNNING' THEN 1
                    WHEN UPPER(LTRIM(RTRIM(CONVERT(NVARCHAR(20), Status)))) = 'FINISHED' THEN 2
                    ELSE 0
                END AS Status,
                DurationInMinutes AS DurationInMinuts,
                StartAtUtc AS StartsAtUtc,
                EndAtUtc AS EndsAtUtc,
                CreatedAtUtc,
                FinishedAtUtc
            FROM EventWordWars
            WHERE Id = @WarId;";

        return db.QueryFirstOrDefaultAsync<EventWordWarsDto>(sql, new { WarId = warId }, ct);
    }

    public Task<EventWordWarsDto?> GetActiveByEventIdAsync(Guid eventId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT TOP 1
                Id,
                EventId,
                CreatedByUserId,
                CASE
                    WHEN TRY_CONVERT(INT, Status) IS NOT NULL THEN TRY_CONVERT(INT, Status)
                    WHEN UPPER(LTRIM(RTRIM(CONVERT(NVARCHAR(20), Status)))) = 'WAITING' THEN 0
                    WHEN UPPER(LTRIM(RTRIM(CONVERT(NVARCHAR(20), Status)))) = 'RUNNING' THEN 1
                    WHEN UPPER(LTRIM(RTRIM(CONVERT(NVARCHAR(20), Status)))) = 'FINISHED' THEN 2
                    ELSE 0
                END AS Status,
                DurationInMinutes AS DurationInMinuts,
                StartAtUtc AS StartsAtUtc,
                EndAtUtc AS EndsAtUtc,
                CreatedAtUtc,
                FinishedAtUtc
            FROM EventWordWars
            WHERE EventId = @EventId
              AND (
                    TRY_CONVERT(INT, Status) IN (0, 1)
                    OR UPPER(LTRIM(RTRIM(CONVERT(NVARCHAR(20), Status)))) IN ('WAITING', 'RUNNING')
              )
            ORDER BY CreatedAtUtc DESC;";

        return db.QueryFirstOrDefaultAsync<EventWordWarsDto>(sql, new { EventId = eventId }, ct);
    }
}
