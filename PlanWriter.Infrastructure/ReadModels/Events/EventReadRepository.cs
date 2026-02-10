using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Infrastructure.Data;
using IEventReadRepository = PlanWriter.Domain.Interfaces.ReadModels.Events.IEventReadRepository;

namespace PlanWriter.Infrastructure.ReadModels.Events;

public class EventReadRepository(IDbExecutor db) : IEventReadRepository
{
    public Task<IReadOnlyList<EventDto>> GetActiveAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                Id,
                Name,
                Slug,
                CAST([Type] AS varchar(50)) AS [Type],
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                IsActive
            FROM Events
            WHERE IsActive = 1
              AND StartsAtUtc <= SYSUTCDATETIME()
              AND EndsAtUtc   >= SYSUTCDATETIME()
            ORDER BY StartsAtUtc;
        ";

        return db.QueryAsync<EventDto>(sql, param: null, ct: cancellationToken);
    }

    public Task<EventDto?> GetEventByIdAsync(Guid requestEventId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                Id,
                Name,
                Slug,
                [Type],
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                IsActive
            FROM Events
            WHERE Id = @EventId;
        ";

        return db.QueryFirstOrDefaultAsync<EventDto>(sql, new { EventId = requestEventId }, ct: cancellationToken);
    }
}