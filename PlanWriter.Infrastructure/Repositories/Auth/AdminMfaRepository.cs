using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.Auth;

public sealed class AdminMfaRepository(IDbExecutor db) : IAdminMfaRepository
{
    public Task SetPendingSecretAsync(Guid userId, string pendingSecret, DateTime generatedAtUtc, CancellationToken ct)
    {
        const string sql = @"
            UPDATE Users
            SET
                AdminMfaPendingSecret = @PendingSecret,
                AdminMfaPendingGeneratedAtUtc = @GeneratedAtUtc
            WHERE Id = @UserId
              AND IsAdmin = 1;
        ";

        return db.ExecuteAsync(sql, new
        {
            UserId = userId,
            PendingSecret = pendingSecret,
            GeneratedAtUtc = generatedAtUtc
        }, ct);
    }

    public Task EnableAsync(Guid userId, string activeSecret, CancellationToken ct)
    {
        const string sql = @"
            UPDATE Users
            SET
                AdminMfaEnabled = 1,
                AdminMfaSecret = @ActiveSecret,
                AdminMfaPendingSecret = NULL,
                AdminMfaPendingGeneratedAtUtc = NULL
            WHERE Id = @UserId
              AND IsAdmin = 1;
        ";

        return db.ExecuteAsync(sql, new
        {
            UserId = userId,
            ActiveSecret = activeSecret
        }, ct);
    }

    public async Task ReplaceBackupCodesAsync(Guid userId, IReadOnlyCollection<string> codeHashes, CancellationToken ct)
    {
        const string deleteSql = @"
            DELETE FROM AdminMfaBackupCodes
            WHERE UserId = @UserId;
        ";

        await db.ExecuteAsync(deleteSql, new { UserId = userId }, ct);

        if (codeHashes.Count == 0)
        {
            return;
        }

        const string insertSql = @"
            INSERT INTO AdminMfaBackupCodes
            (
                Id,
                UserId,
                CodeHash,
                IsUsed,
                CreatedAtUtc,
                UsedAtUtc
            )
            VALUES
            (
                @Id,
                @UserId,
                @CodeHash,
                0,
                SYSUTCDATETIME(),
                NULL
            );
        ";

        var rows = codeHashes.Select(codeHash => new
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = codeHash
        }).ToArray();

        await db.ExecuteAsync(insertSql, rows, ct);
    }

    public async Task<bool> ConsumeBackupCodeAsync(Guid userId, string codeHash, DateTime usedAtUtc, CancellationToken ct)
    {
        const string sql = @"
            UPDATE TOP (1) AdminMfaBackupCodes
            SET
                IsUsed = 1,
                UsedAtUtc = @UsedAtUtc
            WHERE UserId = @UserId
              AND CodeHash = @CodeHash
              AND IsUsed = 0;
        ";

        var affected = await db.ExecuteAsync(sql, new
        {
            UserId = userId,
            CodeHash = codeHash,
            UsedAtUtc = usedAtUtc
        }, ct);

        return affected == 1;
    }
}
