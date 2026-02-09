using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;

namespace PlanWriter.Application.Events.Queries;

public class GetActiveEventsQueryHandler(IEventReadRepository readRepository, ILogger<GetActiveEventsQueryHandler> logger
) : IRequestHandler<GetActiveEventsQuery, IReadOnlyList<EventDto>>
{
    public async Task<IReadOnlyList<EventDto>> Handle(GetActiveEventsQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting active events");

        var rows = await readRepository.GetActiveAsync(ct);

        logger.LogInformation("Found {Count} active events", rows.Count);

        return rows;
    }
}