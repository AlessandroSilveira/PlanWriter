using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Auth;

public class UserAuthReadRepository(IDbExecutor db) : IUserAuthReadRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        const string sql = @"
        SELECT
            Id,
            Email,
            PasswordHash,
            IsAdmin,
            MustChangePassword
        FROM Users
        WHERE Email = @Email;
    ";

        return db.QueryFirstOrDefaultAsync<User>(sql, new { Email = email }, ct);
    }

}