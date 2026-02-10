using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Events;

namespace PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;

public interface IProjectEventsReadRepository
{
    Task<ProjectEvent?> GetByProjectAndEventWithEventAsync(Guid projectId, Guid eventId, CancellationToken ct);
    Task<ProjectEvent?> GetMostRecentWinByUserIdAsync(Guid userId, CancellationToken ct);
    Task<ProjectEvent?> GetByIdWithEventAsync(Guid projectEventId, CancellationToken ct);
    Task<IReadOnlyList<ProjectEvent>> GetByUserIdAsync(Guid userId, CancellationToken ct);
}