using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectDraftRepository
{
    Task<ProjectDraftDto> UpsertAsync(
        Guid projectId,
        Guid userId,
        string htmlContent,
        DateTime updatedAtUtc,
        DateTime? lastKnownUpdatedAtUtc,
        CancellationToken ct);
}
