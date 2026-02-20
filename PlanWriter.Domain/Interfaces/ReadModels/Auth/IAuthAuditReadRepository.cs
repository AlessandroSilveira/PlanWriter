using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Auth;

namespace PlanWriter.Domain.Interfaces.ReadModels.Auth;

public interface IAuthAuditReadRepository
{
    Task<IReadOnlyList<AuthAuditLogDto>> GetAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? userId,
        string? eventType,
        string? result,
        int limit,
        CancellationToken ct);
}
