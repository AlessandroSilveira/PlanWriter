using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.AdminEvents.Queries;

public class GetActiveQueryHandler(IEventRepository eventRepository, ILogger<GetActiveQueryHandler> logger) : IRequestHandler<GetActiveQuery, List<EventDto>>
{
    public async Task<List<EventDto>> Handle(GetActiveQuery request, CancellationToken cancellationToken)
    {
        var eventsList = await eventRepository.GetActiveEvents();

        if (eventsList.Count == 0)
        {
            logger.LogWarning("No active events found");
            return [];
        }
        logger.LogWarning("Active events found");
        return eventsList;
    }
}
