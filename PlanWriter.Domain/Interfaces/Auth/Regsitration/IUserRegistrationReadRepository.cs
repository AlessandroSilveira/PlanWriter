using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Domain.Interfaces.Auth.Regsitration;

public interface IUserRegistrationReadRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
}