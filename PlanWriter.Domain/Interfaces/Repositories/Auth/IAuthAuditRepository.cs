using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Domain.Interfaces.Repositories.Auth;

public interface IAuthAuditRepository
{
    Task CreateAsync(
        Guid? userId,
        string eventType,
        string result,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        string? correlationId,
        string? details,
        CancellationToken ct);
}
