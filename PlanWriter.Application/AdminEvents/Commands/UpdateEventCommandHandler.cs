using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.AdminEvents.Commands;

public class UpdateEventCommandHandler(IEventRepository eventRepository, ILogger<UpdateEventCommandHandler> logger) : IRequestHandler<UpdateEventCommand, Unit>
{
    public async Task<Unit> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var ev = await eventRepository.GetEventById(request.Id);

            if (ev is null)
            {
                logger.LogWarning("Event {EventId} not found", request.Id);
                throw new InvalidOperationException("Event not found");
            }
        
            var type = ConvertRequestTypeToEventType(request);
        
            PopulateEventFields(request, ev, type);

            await eventRepository.UpdateAsync(ev, request.Id);

            logger.LogInformation("Event {EventId} updated", request.Id);
            return Unit.Value;
        }
        catch (Exception e)
        {
           logger.LogError(e, "Error updating event {EventId}", request.Id);
            throw new Exception("Error updating event");
        }
       
    }

    private static void PopulateEventFields(UpdateEventCommand request, Event ev, EventType type)
    {
        ev.Name = request.Request.Name.Trim();
        ev.Slug = GenerateSlug(request.Request.Name);
        ev.Type = type;
        ev.StartsAtUtc = request.Request.StartDate;
        ev.EndsAtUtc   = request.Request.EndDate;
        ev.IsActive = request.Request.IsActive;
        ev.DefaultTargetWords = request.Request.TargetWords;
    }

    private static EventType ConvertRequestTypeToEventType(UpdateEventCommand request)
    {
        var type = Enum.TryParse<EventType>(request.Request.Type, true, out var t) ? t : EventType.Nanowrimo;
        return type;
    }

    private static string GenerateSlug(string name)
    {
        return name
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "");
    }
}