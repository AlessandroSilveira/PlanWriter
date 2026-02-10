using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectProgressRepository
{
    Task<ProjectProgress> AddProgressAsync(ProjectProgress progress, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<IReadOnlyList<ProjectProgress>> GetByProjectAndDateRangeAsync(Guid projectId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
}
