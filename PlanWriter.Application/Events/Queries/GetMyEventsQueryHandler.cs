using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Queries;

public class GetMyEventsQueryHandler(IEventRepository eventRepository, ILogger<GetMyEventsQueryHandler> logger)
    : IRequestHandler<GetMyEventsQuery, List<MyEventDto>>
{
    public async Task<List<MyEventDto>> Handle(GetMyEventsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting events for user {UserId}", request.UserId);

        var eventsForUser = await eventRepository.GetEventByUserId(request.UserId);

        logger.LogInformation("Found {Count} events for user {UserId}", eventsForUser.Count, request.UserId);

        CalculateCompletionPercentage(eventsForUser);

        logger.LogInformation("Returning events with calculated completion percentage for user {UserId}", request.UserId);

        return eventsForUser;
    }

    /* ===================== PRIVATE METHODS ===================== */

    private static void CalculateCompletionPercentage(IEnumerable<MyEventDto> events)
    {
        foreach (var eventDto in events)
            eventDto.Percent = CalculatePercent(eventDto.TotalWrittenInEvent, eventDto.TargetWords);
    }

    private static int CalculatePercent(int? totalWritten, int? targetWords)
    {
        if (targetWords <= 0)
            return 0;

        return (int)Math.Round(
            (decimal)totalWritten.Value / targetWords.Value * 100
        );
    }
}
