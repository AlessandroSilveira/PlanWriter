using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.Auth;

public sealed class RefreshTokenRepository(IDbExecutor db) : IRefreshTokenRepository
{
    public Task CreateAsync(RefreshTokenSession session, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO RefreshTokenSessions
            (
                Id,
                UserId,
                FamilyId,
                ParentTokenId,
                ReplacedByTokenId,
                TokenHash,
                Device,
                CreatedByIp,
                CreatedAtUtc,
                ExpiresAtUtc,
                LastUsedAtUtc,
                RevokedAtUtc,
                RevokedReason
            )
            VALUES
            (
                @Id,
                @UserId,
                @FamilyId,
                @ParentTokenId,
                @ReplacedByTokenId,
                @TokenHash,
                @Device,
                @CreatedByIp,
                @CreatedAtUtc,
                @ExpiresAtUtc,
                @LastUsedAtUtc,
                @RevokedAtUtc,
                @RevokedReason
            );
        ";

        return db.ExecuteAsync(sql, session, ct);
    }

    public Task<RefreshTokenSession?> GetByHashAsync(string tokenHash, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                Id,
                UserId,
                FamilyId,
                ParentTokenId,
                ReplacedByTokenId,
                TokenHash,
                Device,
                CreatedByIp,
                CreatedAtUtc,
                ExpiresAtUtc,
                LastUsedAtUtc,
                RevokedAtUtc,
                RevokedReason
            FROM RefreshTokenSessions
            WHERE TokenHash = @TokenHash;
        ";

        return db.QueryFirstOrDefaultAsync<RefreshTokenSession>(sql, new { TokenHash = tokenHash }, ct);
    }

    public async Task<bool> MarkRotatedAsync(
        Guid tokenId,
        Guid replacedByTokenId,
        DateTime revokedAtUtc,
        CancellationToken ct)
    {
        const string sql = @"
            UPDATE RefreshTokenSessions
            SET RevokedAtUtc = @RevokedAtUtc,
                RevokedReason = @RevokedReason,
                ReplacedByTokenId = @ReplacedByTokenId
            WHERE Id = @TokenId
              AND RevokedAtUtc IS NULL;
        ";

        var affected = await db.ExecuteAsync(sql, new
        {
            TokenId = tokenId,
            ReplacedByTokenId = replacedByTokenId,
            RevokedAtUtc = revokedAtUtc,
            RevokedReason = "Rotated"
        }, ct);

        return affected == 1;
    }

    public async Task<bool> RevokeAsync(Guid tokenId, DateTime revokedAtUtc, string reason, CancellationToken ct)
    {
        const string sql = @"
            UPDATE RefreshTokenSessions
            SET RevokedAtUtc = @RevokedAtUtc,
                RevokedReason = @Reason
            WHERE Id = @TokenId
              AND RevokedAtUtc IS NULL;
        ";

        var affected = await db.ExecuteAsync(sql, new
        {
            TokenId = tokenId,
            RevokedAtUtc = revokedAtUtc,
            Reason = reason
        }, ct);

        return affected == 1;
    }

    public Task<int> RevokeFamilyAsync(Guid familyId, DateTime revokedAtUtc, string reason, CancellationToken ct)
    {
        const string sql = @"
            UPDATE RefreshTokenSessions
            SET RevokedAtUtc = @RevokedAtUtc,
                RevokedReason = @Reason
            WHERE FamilyId = @FamilyId
              AND RevokedAtUtc IS NULL;
        ";

        return db.ExecuteAsync(sql, new
        {
            FamilyId = familyId,
            RevokedAtUtc = revokedAtUtc,
            Reason = reason
        }, ct);
    }

    public Task<int> RevokeAllByUserAsync(Guid userId, DateTime revokedAtUtc, string reason, CancellationToken ct)
    {
        const string sql = @"
            UPDATE RefreshTokenSessions
            SET RevokedAtUtc = @RevokedAtUtc,
                RevokedReason = @Reason
            WHERE UserId = @UserId
              AND RevokedAtUtc IS NULL;
        ";

        return db.ExecuteAsync(sql, new
        {
            UserId = userId,
            RevokedAtUtc = revokedAtUtc,
            Reason = reason
        }, ct);
    }

    public async Task<bool> UpdateLastUsedAsync(Guid tokenId, DateTime lastUsedAtUtc, CancellationToken ct)
    {
        const string sql = @"
            UPDATE RefreshTokenSessions
            SET LastUsedAtUtc = @LastUsedAtUtc
            WHERE Id = @TokenId;
        ";

        var affected = await db.ExecuteAsync(sql, new
        {
            TokenId = tokenId,
            LastUsedAtUtc = lastUsedAtUtc
        }, ct);

        return affected == 1;
    }
}
