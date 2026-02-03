using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Auth;

public class UserReadRepository(IDbExecutor db) : IUserReadRepository
{
    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                Id,
                Email,
                PasswordHash,
                Role,
                IsActive
            FROM Users
            WHERE Id = @UserId;
        ";

        return db.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId }, ct);
    }
}