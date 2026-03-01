using System;
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
    IEventLifecycleService eventLifecycleService,
    ILogger<GetMyEventsQueryHandler> logger)
    : IRequestHandler<GetMyEventsQuery, List<MyEventDto>>
{
    private const string EventStatusScheduled = "scheduled";
    private const string EventStatusActive = "active";
    private const string EventStatusClosed = "closed";
    private const string EventStatusDisabled = "disabled";

    public async Task<List<MyEventDto>> Handle(GetMyEventsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting events for user {UserId}", request.UserId);

        await eventLifecycleService.SyncExpiredEventsAsync(cancellationToken);

        var eventsForUser = await eventRepository.GetEventByUserId(request.UserId);

        logger.LogInformation("Found {Count} events for user {UserId}", eventsForUser.Count, request.UserId);

        ApplyCalculatedMetrics(eventsForUser, DateTime.UtcNow);

        logger.LogInformation("Returning events with calculated completion percentage for user {UserId}", request.UserId);

        return eventsForUser;
    }

    /* ===================== PRIVATE METHODS ===================== */

    private void ApplyCalculatedMetrics(IEnumerable<MyEventDto> events, DateTime nowUtc)
    {
        foreach (var eventDto in events)
        {
            ApplyEffectiveStatus(eventDto, nowUtc);
            ApplyFinalSnapshotWordsIfAvailable(eventDto);

            var metrics = eventProgressCalculator.Calculate(
                eventDto.TargetWords,
                eventDto.EventDefaultTargetWords,
                eventDto.TotalWrittenInEvent);
            eventDto.TargetWords = metrics.TargetWords;
            eventDto.TotalWrittenInEvent = metrics.TotalWords;
            eventDto.Percent = metrics.Percent;
            eventDto.Won = metrics.Won;

            ApplyFinalSnapshotWinnerIfAvailable(eventDto);
        }
    }

    private static void ApplyEffectiveStatus(MyEventDto dto, DateTime nowUtc)
    {
        if (dto.EventIsActive is null || dto.StartsAtUtc is null || dto.EndsAtUtc is null)
            return;

        var effectiveStatus = ResolveEffectiveStatus(nowUtc, dto.EventIsActive.Value, dto.StartsAtUtc.Value, dto.EndsAtUtc.Value);
        dto.EffectiveStatus = effectiveStatus;
        dto.IsEffectivelyActive = effectiveStatus == EventStatusActive;
    }

    private static void ApplyFinalSnapshotWordsIfAvailable(MyEventDto dto)
    {
        if (dto.ValidatedAtUtc is null)
            return;

        var effectiveStatus = dto.EffectiveStatus;
        if (!string.Equals(effectiveStatus, EventStatusClosed, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(effectiveStatus, EventStatusDisabled, StringComparison.OrdinalIgnoreCase))
            return;

        var snapshotWords = dto.ValidatedWordsSnapshot ?? dto.FinalWordCountSnapshot;
        if (snapshotWords is not null)
            dto.TotalWrittenInEvent = snapshotWords;
    }

    private static void ApplyFinalSnapshotWinnerIfAvailable(MyEventDto dto)
    {
        if (dto.ValidatedAtUtc is null)
            return;

        var effectiveStatus = dto.EffectiveStatus;
        if (!string.Equals(effectiveStatus, EventStatusClosed, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(effectiveStatus, EventStatusDisabled, StringComparison.OrdinalIgnoreCase))
            return;

        if (dto.PersistedWon.HasValue)
            dto.Won = dto.PersistedWon.Value;
    }

    private static string ResolveEffectiveStatus(DateTime nowUtc, bool eventIsActive, DateTime startsAtUtc, DateTime endsAtUtc)
    {
        if (!eventIsActive)
            return EventStatusDisabled;

        if (nowUtc < startsAtUtc)
            return EventStatusScheduled;

        if (nowUtc > endsAtUtc)
            return EventStatusClosed;

        return EventStatusActive;
    }
}
