using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public sealed class EventValidationAuditRepository(IDbExecutor db) : IEventValidationAuditRepository
{
    public Task CreateAsync(
        Guid eventId,
        Guid projectId,
        Guid userId,
        string source,
        int submittedWords,
        string status,
        DateTime? validatedAtUtc,
        string? reason,
        CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO EventValidationAudits
            (
                Id,
                EventId,
                ProjectId,
                UserId,
                Source,
                SubmittedWords,
                Status,
                ValidatedAtUtc,
                Reason,
                CreatedAtUtc
            )
            VALUES
            (
                @Id,
                @EventId,
                @ProjectId,
                @UserId,
                @Source,
                @SubmittedWords,
                @Status,
                @ValidatedAtUtc,
                @Reason,
                SYSUTCDATETIME()
            );
        ";

        return db.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ProjectId = projectId,
            UserId = userId,
            Source = source,
            SubmittedWords = submittedWords,
            Status = status,
            ValidatedAtUtc = validatedAtUtc,
            Reason = reason
        }, ct);
    }
}
