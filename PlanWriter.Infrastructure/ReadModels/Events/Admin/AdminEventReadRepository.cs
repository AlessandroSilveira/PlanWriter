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
                CASE
                    WHEN TRY_CONVERT(INT, [Type]) = 0 THEN 'Nanowrimo'
                    WHEN TRY_CONVERT(INT, [Type]) = 1 THEN 'Desafio'
                    WHEN TRY_CONVERT(INT, [Type]) = 2 THEN 'Oficial'
                    WHEN NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(50), [Type]))), '') IS NOT NULL
                        THEN CONVERT(NVARCHAR(50), [Type])
                    ELSE 'Nanowrimo'
                END AS Type,
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                IsActive,
                ValidationWindowStartsAtUtc,
                ValidationWindowEndsAtUtc,
                AllowedValidationSources
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
                CASE
                    WHEN TRY_CONVERT(INT, [Type]) = 0 THEN 'Nanowrimo'
                    WHEN TRY_CONVERT(INT, [Type]) = 1 THEN 'Desafio'
                    WHEN TRY_CONVERT(INT, [Type]) = 2 THEN 'Oficial'
                    WHEN NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(50), [Type]))), '') IS NOT NULL
                        THEN CONVERT(NVARCHAR(50), [Type])
                    ELSE 'Nanowrimo'
                END AS Type,
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                IsActive,
                ValidationWindowStartsAtUtc,
                ValidationWindowEndsAtUtc,
                AllowedValidationSources
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
