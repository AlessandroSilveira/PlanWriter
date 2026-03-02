using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Domain.Interfaces.ReadModels.Projects;

public interface IProjectDraftReadRepository
{
    Task<ProjectDraftDto?> GetByProjectIdAsync(Guid projectId, Guid userId, CancellationToken ct);
}
