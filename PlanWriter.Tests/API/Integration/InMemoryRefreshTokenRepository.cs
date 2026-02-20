using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Tests.API.Integration;

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, RefreshTokenSession> _sessions = new();

    public void Reset()
    {
        lock (_lock)
        {
            _sessions.Clear();
        }
    }

    public Task CreateAsync(RefreshTokenSession session, CancellationToken ct)
    {
        lock (_lock)
        {
            _sessions[session.Id] = Clone(session);
        }

        return Task.CompletedTask;
    }

    public Task<RefreshTokenSession?> GetByHashAsync(string tokenHash, CancellationToken ct)
    {
        lock (_lock)
        {
            var session = _sessions.Values.FirstOrDefault(x => x.TokenHash == tokenHash);
            return Task.FromResult(session is null ? null : Clone(session));
        }
    }

    public Task<bool> MarkRotatedAsync(Guid tokenId, Guid replacedByTokenId, DateTime revokedAtUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(tokenId, out var session) || session.RevokedAtUtc.HasValue)
            {
                return Task.FromResult(false);
            }

            session.RevokedAtUtc = revokedAtUtc;
            session.ReplacedByTokenId = replacedByTokenId;
            session.RevokedReason = "Rotated";
            return Task.FromResult(true);
        }
    }

    public Task<bool> RevokeAsync(Guid tokenId, DateTime revokedAtUtc, string reason, CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(tokenId, out var session) || session.RevokedAtUtc.HasValue)
            {
                return Task.FromResult(false);
            }

            session.RevokedAtUtc = revokedAtUtc;
            session.RevokedReason = reason;
            return Task.FromResult(true);
        }
    }

    public Task<int> RevokeFamilyAsync(Guid familyId, DateTime revokedAtUtc, string reason, CancellationToken ct)
    {
        lock (_lock)
        {
            var affected = 0;
            foreach (var session in _sessions.Values.Where(x => x.FamilyId == familyId && !x.RevokedAtUtc.HasValue))
            {
                session.RevokedAtUtc = revokedAtUtc;
                session.RevokedReason = reason;
                affected++;
            }

            return Task.FromResult(affected);
        }
    }

    public Task<int> RevokeAllByUserAsync(Guid userId, DateTime revokedAtUtc, string reason, CancellationToken ct)
    {
        lock (_lock)
        {
            var affected = 0;
            foreach (var session in _sessions.Values.Where(x => x.UserId == userId && !x.RevokedAtUtc.HasValue))
            {
                session.RevokedAtUtc = revokedAtUtc;
                session.RevokedReason = reason;
                affected++;
            }

            return Task.FromResult(affected);
        }
    }

    public Task<bool> UpdateLastUsedAsync(Guid tokenId, DateTime lastUsedAtUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(tokenId, out var session))
            {
                return Task.FromResult(false);
            }

            session.LastUsedAtUtc = lastUsedAtUtc;
            return Task.FromResult(true);
        }
    }

    private static RefreshTokenSession Clone(RefreshTokenSession session)
    {
        return new RefreshTokenSession
        {
            Id = session.Id,
            UserId = session.UserId,
            FamilyId = session.FamilyId,
            ParentTokenId = session.ParentTokenId,
            ReplacedByTokenId = session.ReplacedByTokenId,
            TokenHash = session.TokenHash,
            Device = session.Device,
            CreatedByIp = session.CreatedByIp,
            CreatedAtUtc = session.CreatedAtUtc,
            ExpiresAtUtc = session.ExpiresAtUtc,
            LastUsedAtUtc = session.LastUsedAtUtc,
            RevokedAtUtc = session.RevokedAtUtc,
            RevokedReason = session.RevokedReason
        };
    }
}
