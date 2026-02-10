using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Events;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectEventsRepository
{
    Task<ProjectEvent?> GetByProjectAndEventAsync(Guid projectId, Guid eventId, CancellationToken ct);
    Task CreateAsync(ProjectEvent entity, CancellationToken ct);
    Task UpdateTargetWordsAsync(Guid projectEventId, int targetWords, CancellationToken ct);
    Task<bool> RemoveByKeysAsync(Guid projectId, Guid eventId, CancellationToken ct);
    Task UpdateProjectEvent(ProjectEvent projectEvent, CancellationToken cancellationToken);
    Task RemoveByKeys(Guid requestProjectId, Guid requestEventId);
}