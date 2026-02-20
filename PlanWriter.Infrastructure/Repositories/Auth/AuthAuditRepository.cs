using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.Auth;

public sealed class AuthAuditRepository(IDbExecutor db) : IAuthAuditRepository
{
    public Task CreateAsync(
        Guid? userId,
        string eventType,
        string result,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        string? correlationId,
        string? details,
        CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO AuthAuditLogs
            (
                Id,
                UserId,
                EventType,
                Result,
                IpAddress,
                UserAgent,
                TraceId,
                CorrelationId,
                Details,
                CreatedAtUtc
            )
            VALUES
            (
                @Id,
                @UserId,
                @EventType,
                @Result,
                @IpAddress,
                @UserAgent,
                @TraceId,
                @CorrelationId,
                @Details,
                SYSUTCDATETIME()
            );
        ";

        return db.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            Result = result,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            TraceId = traceId,
            CorrelationId = correlationId,
            Details = details
        }, ct);
    }
}
