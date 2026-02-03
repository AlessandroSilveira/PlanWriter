using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.Auth;

public class UserPasswordRepository(IDbExecutor db) : IUserPasswordRepository
{

    public async Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken ct)
    {
        const string sql = @"
            UPDATE Users
            SET PasswordHash = @PasswordHash,
                MustChangePassword = 0
            WHERE Id = @UserId;
        ";

        var affected = await db.ExecuteAsync(
            sql,
            new
            {
                UserId = userId,
                PasswordHash = passwordHash
            },
            ct
        );

        if (affected != 1)
            throw new InvalidOperationException(
                $"Update Users.PasswordHash expected 1 row, affected={affected}."
            );
    }
}