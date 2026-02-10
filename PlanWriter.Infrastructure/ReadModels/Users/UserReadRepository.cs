using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Users;

public sealed class UserReadRepository(IDbExecutor db) : IUserReadRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        const string sql = @"SELECT 1 FROM Users WHERE Email = @Email;";
        return db.QueryFirstOrDefaultAsync<int?>(sql, new { Email = email }, ct)
            .ContinueWith(t => t.Result.HasValue, ct);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        const string sql = @"SELECT TOP 1 * FROM Users WHERE Email = @Email;";
        return db.QueryFirstOrDefaultAsync<User>(sql, new { Email = email }, ct);
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"SELECT TOP 1 * FROM Users WHERE Id = @Id;";
        return db.QueryFirstOrDefaultAsync<User>(sql, new { Id = userId }, ct);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT 1
            FROM Users
            WHERE Slug = @Slug
              AND Id <> @UserId;
        ";

        return db.QueryFirstOrDefaultAsync<int?>(sql, new { Slug = slug, UserId = userId }, ct)
            .ContinueWith(t => t.Result.HasValue, ct);
    }

    public Task<User?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        const string sql = @"SELECT TOP 1 * FROM Users WHERE Slug = @Slug;";
        return db.QueryFirstOrDefaultAsync<User>(sql, new { Slug = slug }, ct);
    }

    public async Task<IReadOnlyList<User>> GetUsersByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        const string sql = @"SELECT * FROM Users WHERE Id IN @Ids;";
        var rows = await db.QueryAsync<User>(sql, new { Ids = ids }, ct);
        return rows.ToList();
    }
}