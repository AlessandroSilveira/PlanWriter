using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;

public interface IEventReadRepository
{
    Task<IReadOnlyList<EventDto>> GetActiveAsync(CancellationToken cancellationToken);
}