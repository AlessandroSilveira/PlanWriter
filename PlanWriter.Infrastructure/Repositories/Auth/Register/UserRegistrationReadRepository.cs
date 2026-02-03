using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.Auth.Register;

public class UserRegistrationReadRepository(IDbExecutor db) : IUserRegistrationReadRepository
{
    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        const string sql = @"
            SELECT 1
            FROM Users
            WHERE Email = @Email;
        ";

        var result = await db.QueryFirstOrDefaultAsync<int?>(sql, new { Email = email }, ct);

        return result.HasValue;
    }
}