using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Application.EventValidation;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories.Events.Admin;

namespace PlanWriter.Application.AdminEvents.Commands;

public class UpdateAdminEventCommandHandler(
    IAdminEventReadRepository adminEventReadRepository,
    IAdminEventRepository adminEventRepository,
    IProjectEventsReadRepository projectEventsReadRepository,
    IMediator mediator,
    ILogger<UpdateAdminEventCommandHandler> logger) : IRequestHandler<UpdateAdminEventCommand, Unit>
{
    public async Task<Unit> Handle(UpdateAdminEventCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var ev = await adminEventReadRepository.GetByIdAsync(request.Id, cancellationToken);

            if (ev is null)
            {
                logger.LogWarning("Event {EventId} not found", request.Id);
                throw new InvalidOperationException("Event not found");
            }

            var name = request.Request.Name.Trim();
            var updatedEvent = ev with
            {
                Name = name,
                Slug = GenerateSlug(name),
                Type = ParseEventType(request.Request.Type),
                StartsAtUtc = request.Request.StartDate,
                EndsAtUtc = request.Request.EndDate,
                DefaultTargetWords = request.Request.TargetWords,
                IsActive = request.Request.IsActive,
                ValidationWindowStartsAtUtc = request.Request.ValidationWindowStartsAtUtc,
                ValidationWindowEndsAtUtc = request.Request.ValidationWindowEndsAtUtc,
                AllowedValidationSources = ValidationPolicyHelper.NormalizeAllowedSources(request.Request.AllowedValidationSources)
            };

            await adminEventRepository.UpdateAsync(request.Id, updatedEvent, cancellationToken);
            await FinalizeParticipantsIfEventIsEffectivelyClosedAsync(updatedEvent, cancellationToken);

            logger.LogInformation("Event {EventId} updated", request.Id);
            return Unit.Value;
        }
        catch (Exception e)
        {
           logger.LogError(e, "Error updating event {EventId}", request.Id);
           throw new Exception("Error updating event");
        }
    }

    private async Task FinalizeParticipantsIfEventIsEffectivelyClosedAsync(EventDto updatedEvent, CancellationToken cancellationToken)
    {
        if (!IsEffectivelyClosed(updatedEvent, DateTime.UtcNow))
        {
            return;
        }

        var projectEvents = await projectEventsReadRepository.GetByEventIdAsync(updatedEvent.Id, cancellationToken)
                         ?? Array.Empty<ProjectEvent>();
        if (projectEvents.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "Event {EventId} is effectively closed. Finalizing {Count} participations for stable winner snapshots.",
            updatedEvent.Id,
            projectEvents.Count);

        foreach (var projectEvent in projectEvents)
        {
            try
            {
                await mediator.Send(new FinalizeEventCommand(new FinalizeRequest(projectEvent.Id)), cancellationToken);
            }
            catch (Exception ex)
            {
                // Best-effort batch finalization: event closure should not fail because one participation could not be finalized.
                logger.LogError(
                    ex,
                    "Failed to finalize ProjectEvent {ProjectEventId} while closing Event {EventId}",
                    projectEvent.Id,
                    updatedEvent.Id);
            }
        }
    }

    private static string ParseEventType(string type)
        => Enum.TryParse<EventType>(type, true, out var parsed)
            ? parsed.ToString()
            : EventType.Nanowrimo.ToString();

    private static string GenerateSlug(string name)
        => name
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "");

    private static bool IsEffectivelyClosed(EventDto ev, DateTime nowUtc)
        => !ev.IsActive || ev.EndsAtUtc <= nowUtc;
}
