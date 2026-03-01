using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Common.Events;

public sealed class EventLifecycleService(
    IEventRepository eventRepository,
    IProjectEventsReadRepository projectEventsReadRepository,
    IMediator mediator,
    ILogger<EventLifecycleService> logger) : IEventLifecycleService
{
    public async Task SyncExpiredEventsAsync(CancellationToken cancellationToken)
    {
        var allEvents = await eventRepository.GetAllAsync() ?? [];
        var nowUtc = DateTime.UtcNow;

        foreach (var eventDto in allEvents.Where(x => ShouldClose(x, nowUtc)))
        {
            await CloseAndFinalizeAsync(eventDto.Id, cancellationToken);
        }
    }

    public async Task SyncEventIfExpiredAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetEventById(eventId);
        if (eventEntity is null || !ShouldClose(eventEntity, DateTime.UtcNow))
        {
            return;
        }

        await CloseAndFinalizeAsync(eventId, cancellationToken);
    }

    private async Task CloseAndFinalizeAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetEventById(eventId);
        if (eventEntity is null || !ShouldClose(eventEntity, DateTime.UtcNow))
        {
            return;
        }

        eventEntity.IsActive = false;
        await eventRepository.UpdateAsync(eventEntity, eventEntity.Id);

        var projectEvents = await projectEventsReadRepository.GetByEventIdAsync(eventId, cancellationToken)
                          ?? Array.Empty<ProjectEvent>();

        logger.LogInformation(
            "Event {EventId} expired naturally. Closing event and finalizing {Count} participations.",
            eventId,
            projectEvents.Count);

        foreach (var projectEvent in projectEvents)
        {
            try
            {
                await mediator.Send(new FinalizeEventCommand(new FinalizeRequest(projectEvent.Id)), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to finalize ProjectEvent {ProjectEventId} while auto-closing Event {EventId}",
                    projectEvent.Id,
                    eventId);
            }
        }
    }

    private static bool ShouldClose(EventDto eventDto, DateTime nowUtc)
        => eventDto.IsActive && eventDto.EndsAtUtc <= nowUtc;

    private static bool ShouldClose(Event eventEntity, DateTime nowUtc)
        => eventEntity.IsActive && eventEntity.EndsAtUtc <= nowUtc;
}
