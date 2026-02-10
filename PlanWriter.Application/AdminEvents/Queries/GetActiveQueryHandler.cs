using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using IEventReadRepository = PlanWriter.Domain.Interfaces.ReadModels.Events.IEventReadRepository;

namespace PlanWriter.Application.AdminEvents.Queries;

public class GetActiveEventsQueryHandler(IEventReadRepository eventReadRepository, ILogger<GetActiveEventsQueryHandler> logger)
    : IRequestHandler<GetActiveEventsQuery, IReadOnlyList<EventDto>>
{
    public async Task<IReadOnlyList<EventDto>> Handle(GetActiveEventsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active events");

        var events = await eventReadRepository
            .GetActiveAsync(cancellationToken);

        if (events.Count == 0)
        {
            logger.LogInformation("No active events found");
            return Array.Empty<EventDto>();
        }

        logger.LogInformation("Found {Count} active events", events.Count);
        return events;
    }
}