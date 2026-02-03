using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.ReadModels.Auth;

public interface IUserReadRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken ct);
}