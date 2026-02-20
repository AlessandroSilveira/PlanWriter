using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Tests.API.Integration;

public sealed class InMemoryAuthRepository :
    IUserReadRepository,
    IUserRepository,
    IUserRegistrationReadRepository,
    IUserRegistrationRepository,
    IUserPasswordRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, User> _users = new();

    public void Reset()
    {
        lock (_lock)
        {
            _users.Clear();
        }
    }

    public void Seed(User user)
    {
        lock (_lock)
        {
            _users[user.Id] = user;
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

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}
