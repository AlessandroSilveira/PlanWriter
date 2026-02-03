using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Queries;

public class GetActiveEventsQueryHandler(IEventRepository eventRepository, ILogger<GetActiveEventsQueryHandler> logger) : IRequestHandler<GetActiveEventsQuery, List<EventDto>>
{
    public async Task<List<EventDto>> Handle(GetActiveEventsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active events");
        var activeEventsList = await eventRepository.GetActiveEvents();
        
        logger.LogInformation("Found {Count} active events", activeEventsList.Count);
        return activeEventsList;
    }
}