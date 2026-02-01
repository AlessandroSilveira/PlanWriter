using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.AdminEvents.Queries;

public class GetEventByIdQueryHandler(IEventRepository eventRepository, ILogger<GetEventByIdQueryHandler> logger) : IRequestHandler<GetEventByIdQuery, EventDto?>
{
    public async Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var ev = await eventRepository.GetEventById(request.EventId);
        
        if (ev is null)
        {
            logger.LogWarning("Event {EventId} not found", request.EventId);
            return null;
        }
        
        logger.LogInformation("Event {EventId} retrieved successfully", ev.Id);
        return new EventDto(ev.Id, ev.Name, ev.Slug, ev.Type.ToString(),
                ev.StartsAtUtc, ev.EndsAtUtc, ev.DefaultTargetWords, ev.IsActive);
    }
}