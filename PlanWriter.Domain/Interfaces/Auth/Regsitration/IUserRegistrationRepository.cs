using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Auth.Regsitration;

public interface IUserRegistrationRepository
{
    Task CreateAsync(User user, CancellationToken ct);
}