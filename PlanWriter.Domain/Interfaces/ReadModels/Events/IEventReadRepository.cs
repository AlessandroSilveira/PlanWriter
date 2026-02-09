using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Domain.Interfaces.ReadModels.Events;

public interface IEventReadRepository
{
    Task<IReadOnlyList<EventDto>> GetActiveAsync(CancellationToken ct);
    Task<EventDto?> GetEventByIdAsync(Guid requestEventId, CancellationToken cancellationToken);
}