using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Events;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Queries;

public class GetMyEventsQueryHandler(
    IEventRepository eventRepository,
    IEventProgressCalculator eventProgressCalculator,
    ILogger<GetMyEventsQueryHandler> logger)
    : IRequestHandler<GetMyEventsQuery, List<MyEventDto>>
{
    public async Task<List<MyEventDto>> Handle(GetMyEventsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting events for user {UserId}", request.UserId);

        var eventsForUser = await eventRepository.GetEventByUserId(request.UserId);

        logger.LogInformation("Found {Count} events for user {UserId}", eventsForUser.Count, request.UserId);

        ApplyCalculatedMetrics(eventsForUser);

        logger.LogInformation("Returning events with calculated completion percentage for user {UserId}", request.UserId);

        return eventsForUser;
    }

    /* ===================== PRIVATE METHODS ===================== */

    private void ApplyCalculatedMetrics(IEnumerable<MyEventDto> events)
    {
        foreach (var eventDto in events)
        {
            var metrics = eventProgressCalculator.Calculate(eventDto.TargetWords, eventDto.TotalWrittenInEvent);
            eventDto.TargetWords = metrics.TargetWords;
            eventDto.TotalWrittenInEvent = metrics.TotalWords;
            eventDto.Percent = metrics.Percent;
            eventDto.Won = metrics.Won;
        }
    }
}
