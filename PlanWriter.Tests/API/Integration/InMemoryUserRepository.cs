using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Tests.API.Integration;

public sealed class InMemoryUserRepository(InMemoryProfileStore store)
    : IUserRepository, IUserReadRepository
{
    public Task CreateAsync(User user, CancellationToken ct)
    {
        store.UpsertUser(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken ct)
    {
        store.UpsertUser(user);
        return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        return Task.FromResult(store.EmailExists(email));
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return Task.FromResult(store.GetUserByEmail(email));
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct)
    {
        return Task.FromResult(store.GetUserById(userId));
    }

    public Task<bool> SlugExistsAsync(string slug, Guid userId, CancellationToken ct)
    {
        return Task.FromResult(store.SlugExists(slug, userId));
    }

    public Task<User?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        return Task.FromResult(store.GetUserBySlug(slug));
    }

    public Task<IReadOnlyList<User>> GetUsersByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        return Task.FromResult(store.GetUsersByIds(ids));
    }
}
