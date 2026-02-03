using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;

namespace PlanWriter.Domain.Interfaces.Repositories.Events.Admin;

public interface IAdminEventRepository
{
    Task CreateAsync(Event entity, CancellationToken ct);
    Task UpdateAsync(Guid eventId, EventDto ev, CancellationToken cancellationToken);
    Task DeleteAsync(EventDto entity, CancellationToken ct);
}
