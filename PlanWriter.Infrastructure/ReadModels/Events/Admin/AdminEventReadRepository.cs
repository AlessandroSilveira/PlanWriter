using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Events.Admin;

public class AdminEventReadRepository(IDbExecutor db) : IAdminEventReadRepository
{
    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct)
    {
        const string sql = @"
            SELECT 1
            FROM Events
            WHERE Slug = @Slug;
        ";

        var result = await db.QueryFirstOrDefaultAsync<int?>(sql, new { Slug = slug }, ct);

        return result.HasValue;
    }

    public Task<EventDto?> GetByIdAsync(Guid eventId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                Id,
                Name,
                Slug,
                CASE [Type]
                    WHEN 0 THEN 'Nanowrimo'
                    WHEN 1 THEN 'Desafio'
                    WHEN 2 THEN 'Oficial'
                    ELSE 'Nanowrimo'
                END AS Type,
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                IsActive
            FROM Events
            WHERE Id = @EventId;
        ";

        return db.QueryFirstOrDefaultAsync<EventDto>(sql, new { EventId = eventId }, ct);
    }
    public Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken ct)
    {
        const string sql = @"
            SELECT
                Id,
                Name,
                Slug,
                CASE [Type]
                    WHEN 0 THEN 'Nanowrimo'
                    WHEN 1 THEN 'Desafio'
                    WHEN 2 THEN 'Oficial'
                    ELSE 'Nanowrimo'
                END AS Type,
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                IsActive
            FROM Events
            ORDER BY StartsAtUtc DESC;
        ";

        return db.QueryAsync<EventDto>(
            sql,
            param: null,
            ct: ct
        );
    }
}
