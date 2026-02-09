using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.ReadModels.Users;

public interface IUserReadRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken ct);
    Task<bool> SlugExistsAsync(string slug, Guid userId, CancellationToken ct);
    Task<User?> GetBySlugAsync(string slug, CancellationToken ct);
    Task<IReadOnlyList<User>> GetUsersByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
}