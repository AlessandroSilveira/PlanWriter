using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Auth;

public sealed class AuthAuditReadRepository(IDbExecutor db) : IAuthAuditReadRepository
{
    public Task<IReadOnlyList<AuthAuditLogDto>> GetAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? userId,
        string? eventType,
        string? result,
        int limit,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP (@Limit)
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
            FROM AuthAuditLogs
            WHERE (@FromUtc IS NULL OR CreatedAtUtc >= @FromUtc)
              AND (@ToUtc IS NULL OR CreatedAtUtc <= @ToUtc)
              AND (@UserId IS NULL OR UserId = @UserId)
              AND (@EventType IS NULL OR EventType = @EventType)
              AND (@Result IS NULL OR Result = @Result)
            ORDER BY CreatedAtUtc DESC;
        ";

        return db.QueryAsync<AuthAuditLogDto>(sql, new
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            UserId = userId,
            EventType = string.IsNullOrWhiteSpace(eventType) ? null : eventType.Trim(),
            Result = string.IsNullOrWhiteSpace(result) ? null : result.Trim(),
            Limit = Math.Max(1, limit)
        }, ct);
    }
}
