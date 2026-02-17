using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IEventValidationAuditRepository
{
    Task CreateAsync(
        Guid eventId,
        Guid projectId,
        Guid userId,
        string source,
        int submittedWords,
        string status,
        DateTime? validatedAtUtc,
        string? reason,
        CancellationToken ct);
}
