using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories.Auth;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshTokenSession session, CancellationToken ct);
    Task<RefreshTokenSession?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task<bool> MarkRotatedAsync(Guid tokenId, Guid replacedByTokenId, DateTime revokedAtUtc, CancellationToken ct);
    Task<bool> RevokeAsync(Guid tokenId, DateTime revokedAtUtc, string reason, CancellationToken ct);
    Task<int> RevokeFamilyAsync(Guid familyId, DateTime revokedAtUtc, string reason, CancellationToken ct);
    Task<bool> UpdateLastUsedAsync(Guid tokenId, DateTime lastUsedAtUtc, CancellationToken ct);
}
