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
                Status,
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
                Status,
                DurationInMinutes AS DurationInMinuts,
                StartAtUtc AS StartsAtUtc,
                EndAtUtc AS EndsAtUtc,
                CreatedAtUtc,
                FinishedAtUtc
            FROM EventWordWars
            WHERE EventId = @EventId
              AND Status IN ('Waiting', 'Running')
            ORDER BY CreatedAtUtc DESC;";

        return db.QueryFirstOrDefaultAsync<EventWordWarsDto>(sql, new { EventId = eventId }, ct);
    }
}
