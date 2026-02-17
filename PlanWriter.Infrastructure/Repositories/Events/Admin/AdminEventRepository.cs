using System;
using System.Threading;
using System.Threading.Tasks;
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
                ValidationWindowStartsAtUtc,
                ValidationWindowEndsAtUtc,
                AllowedValidationSources,
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
                @ValidationWindowStartsAtUtc,
                @ValidationWindowEndsAtUtc,
                @AllowedValidationSources,
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
                entity.ValidationWindowStartsAtUtc,
                entity.ValidationWindowEndsAtUtc,
                entity.AllowedValidationSources,
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
            DefaultTargetWords = @DefaultTargetWords,
            ValidationWindowStartsAtUtc = @ValidationWindowStartsAtUtc,
            ValidationWindowEndsAtUtc = @ValidationWindowEndsAtUtc,
            AllowedValidationSources = @AllowedValidationSources,
            IsActive = @IsActive
        WHERE Id = @EventId;
    ";

        var typeValue = Enum.TryParse<EventType>(entity.Type, true, out var parsedType)
            ? (int)parsedType
            : (int)EventType.Nanowrimo;

        var affected = await db.ExecuteAsync(
            sql,
            new
            {
                EventId = eventId,
                entity.Name,
                entity.Slug,
                Type = typeValue,
                entity.StartsAtUtc,
                entity.EndsAtUtc,
                entity.DefaultTargetWords,
                entity.ValidationWindowStartsAtUtc,
                entity.ValidationWindowEndsAtUtc,
                entity.AllowedValidationSources,
                entity.IsActive
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
