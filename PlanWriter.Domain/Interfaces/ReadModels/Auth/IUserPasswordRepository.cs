using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Domain.Interfaces.ReadModels.Auth;

public interface IUserPasswordRepository
{
    Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken ct);
}