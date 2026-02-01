using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.AdminEvents.Queries;

public class GetEventQueryHandler(IEventRepository eventRepository, ILogger<GetEventQueryHandler> logger) : IRequestHandler<GetEventsQuery, List<EventDto>?>
{
    public async Task<List<EventDto>?> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var allEvents = await eventRepository.GetAllAsync();

        if (allEvents != null && allEvents.Count == 0)
        {
            logger.LogInformation("No events found");
            return [];
        }
        
        logger.LogInformation("Events found");
        return allEvents;


    }
}