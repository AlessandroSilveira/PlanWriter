using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories.Events.Admin;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.Events.Admin;

public class AdminEventRepository(IDbExecutor db) : IAdminEventRepository
{
    public async Task CreateAsync(Event entity, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO Events
            (
                Id,
                Name,
                Slug,
                Type,
                StartsAtUtc,
                EndsAtUtc,
                DefaultTargetWords,
                IsActive
            )
            VALUES
            (
                @Id,
                @Name,
                @Slug,
                @Type,
                @StartsAtUtc,
                @EndsAtUtc,
                @DefaultTargetWords,
                @IsActive
            );
        ";

        var affected = await db.ExecuteAsync(
            sql,
            new
            {
                entity.Id,
                entity.Name,
                entity.Slug,
                Type = (int)entity.Type,
                entity.StartsAtUtc,
                entity.EndsAtUtc,
                entity.DefaultTargetWords,
                entity.IsActive
            },
            ct
        );

        if (affected != 1)
            throw new InvalidOperationException($"Insert Events expected 1 row, affected={affected}.");
    }
    
    public async Task UpdateAsync( Guid eventId, EventDto entity,CancellationToken ct)
    {
        const string sql = @"
        UPDATE Events
        SET
            Name = @Name,
            Slug = @Slug,
            Type = @Type,
            StartsAtUtc = @StartsAtUtc,
            EndsAtUtc = @EndsAtUtc,
            DefaultTargetWords = @DefaultTargetWords
        WHERE Id = @EventId;
    ";

        var affected = await db.ExecuteAsync(
            sql,
            new
            {
                EventId = eventId,
                entity.Name,
                entity.Slug,
                Type = entity.Type,
                entity.StartsAtUtc,
                entity.EndsAtUtc,
                entity.DefaultTargetWords
            },
            ct
        );

        if (affected != 1)
            throw new InvalidOperationException($"Update Events expected 1 row, affected={affected}.");
    }
    
    public async Task DeleteAsync(EventDto entity, CancellationToken ct)
    {
        const string sql = @"
        DELETE FROM Events
        WHERE Id = @Id;
    ";

        var affected = await db.ExecuteAsync(
            sql,
            new { entity.Id },
            ct
        );

        if (affected != 1)
            throw new InvalidOperationException($"Delete Events expected 1 row, affected={affected}.");
    }

}