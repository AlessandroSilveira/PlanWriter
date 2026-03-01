using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Application.Common.Events;

public interface IEventLifecycleService
{
    Task SyncExpiredEventsAsync(CancellationToken cancellationToken);
    Task SyncEventIfExpiredAsync(Guid eventId, CancellationToken cancellationToken);
}
