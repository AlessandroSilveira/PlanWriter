using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Queries.Admin;

public class GetAdminEventByIdQueryHandler(IEventRepository eventRepository, ILogger<GetAdminEventByIdQueryHandler> logger)
    : IRequestHandler<GetAdminEventByIdQuery, EventDto?>
{
    public async Task<EventDto?> Handle(GetAdminEventByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting event {EventId}", request.EventId);

        var ev = await eventRepository
            .GetEventById(request.EventId);

        if (ev is null)
        {
            logger.LogInformation("Event {EventId} not found", request.EventId);
            return null;
        }

        logger.LogInformation("Found event {EventId}", ev.Id);

        return MapToDto(ev);
    }

    private static EventDto MapToDto(Domain.Events.Event ev)
    {
        return new EventDto(
            ev.Id,
            ev.Name,
            ev.Slug,
            ev.Type.ToString(),
            ev.StartsAtUtc,
            ev.EndsAtUtc,
            ev.DefaultTargetWords,
            ev.IsActive
        );
    }
}