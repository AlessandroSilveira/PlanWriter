using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;

namespace PlanWriter.Application.Events.Queries;

public class GetEventByIdQueryHandler(ILogger<GetEventByIdQueryHandler> logger, IEventReadRepository eventReadRepository)  : IRequestHandler<GetEventByIdQuery, EventDto?>
{
    public async Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting event {EventId}", request.EventId);
        var eventDto = await eventReadRepository.GetEventByIdAsync(request.EventId, cancellationToken);
        
        if (eventDto is null)
        {
            logger.LogWarning("Event {EventId} not found", request.EventId);
            return null;
        }
        
        logger.LogInformation("Event {EventId} found", request.EventId);
        return eventDto;
    }
}