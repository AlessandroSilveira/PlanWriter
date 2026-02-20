using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Tests.API.Integration;

public sealed class InMemoryAuthRepository :
    IUserReadRepository,
    IUserRepository,
    IUserAuthReadRepository,
    IUserRegistrationReadRepository,
    IUserRegistrationRepository,
    IUserPasswordRepository,
    IAdminMfaRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, User> _users = new();
    private readonly Dictionary<Guid, Dictionary<string, bool>> _backupCodes = new();

    public void Reset()
    {
        lock (_lock)
        {
            _users.Clear();
            _backupCodes.Clear();
        }
    }

    public void Seed(User user)
    {
        lock (_lock)
        {
            _users[user.Id] = user;
            if (!_backupCodes.ContainsKey(user.Id))
            {
                _backupCodes[user.Id] = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public IReadOnlyList<User> SnapshotUsers()
    {
        lock (_lock)
        {
            return _users.Values.ToList();
        }
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = Normalize(email);

        lock (_lock)
        {
            return Task.FromResult(
                _users.Values.Any(u => Normalize(u.Email) == normalizedEmail));
        }
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = Normalize(email);

        lock (_lock)
        {
            var user = _users.Values.FirstOrDefault(u => Normalize(u.Email) == normalizedEmail);
            return Task.FromResult(user);
        }
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct)
    {
        lock (_lock)
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }
    }

    public Task<bool> SlugExistsAsync(string slug, Guid userId, CancellationToken ct)
    {
        var normalizedSlug = slug?.Trim().ToLowerInvariant();

        lock (_lock)
        {
            var exists = _users.Values.Any(u =>
                u.Id != userId &&
                !string.IsNullOrWhiteSpace(u.Slug) &&
                u.Slug!.Trim().ToLowerInvariant() == normalizedSlug);

            return Task.FromResult(exists);
        }
    }

    public Task<User?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var normalizedSlug = slug?.Trim().ToLowerInvariant();

        lock (_lock)
        {
            var user = _users.Values.FirstOrDefault(u =>
                !string.IsNullOrWhiteSpace(u.Slug) &&
                u.Slug!.Trim().ToLowerInvariant() == normalizedSlug);

            return Task.FromResult(user);
        }
    }

    public Task<IReadOnlyList<User>> GetUsersByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var idSet = ids.ToHashSet();

        lock (_lock)
        {
            var result = _users.Values.Where(u => idSet.Contains(u.Id)).ToList();
            return Task.FromResult<IReadOnlyList<User>>(result);
        }
    }

    public Task CreateAsync(User user, CancellationToken ct)
    {
        lock (_lock)
        {
            _users[user.Id] = user;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken ct)
    {
        lock (_lock)
        {
            _users[user.Id] = user;
        }

        return Task.CompletedTask;
    }

    public Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_users.TryGetValue(userId, out var user))
            {
                user.PasswordHash = passwordHash;
            }
        }

        return Task.CompletedTask;
    }

    public Task SetPendingSecretAsync(Guid userId, string pendingSecret, DateTime generatedAtUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_users.TryGetValue(userId, out var user) && user.IsAdmin)
            {
                user.SetAdminMfaPending(pendingSecret, generatedAtUtc);
            }
        }

        return Task.CompletedTask;
    }

    public Task EnableAsync(Guid userId, string activeSecret, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_users.TryGetValue(userId, out var user) && user.IsAdmin)
            {
                user.EnableAdminMfa(activeSecret);
            }
        }

        return Task.CompletedTask;
    }

    public Task ReplaceBackupCodesAsync(Guid userId, IReadOnlyCollection<string> codeHashes, CancellationToken ct)
    {
        lock (_lock)
        {
            _backupCodes[userId] = codeHashes.ToDictionary(k => k, _ => false, StringComparer.OrdinalIgnoreCase);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ConsumeBackupCodeAsync(Guid userId, string codeHash, DateTime usedAtUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_backupCodes.TryGetValue(userId, out var codes) &&
                codes.TryGetValue(codeHash, out var isUsed) &&
                !isUsed)
            {
                codes[codeHash] = true;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}
