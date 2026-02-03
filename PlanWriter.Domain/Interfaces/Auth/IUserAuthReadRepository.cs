using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Auth;

public interface IUserAuthReadRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
}