using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Domain.Interfaces.Repositories.Auth;

public interface IAdminMfaRepository
{
    Task SetPendingSecretAsync(Guid userId, string pendingSecret, DateTime generatedAtUtc, CancellationToken ct);
    Task EnableAsync(Guid userId, string activeSecret, CancellationToken ct);
    Task ReplaceBackupCodesAsync(Guid userId, IReadOnlyCollection<string> codeHashes, CancellationToken ct);
    Task<bool> ConsumeBackupCodeAsync(Guid userId, string codeHash, DateTime usedAtUtc, CancellationToken ct);
}
