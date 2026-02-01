using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.AdminEvents.Commands;

public class CreateEventCommandHandler(
    IEventRepository eventRepository,
    ILogger<CreateEventCommandHandler> logger)
    : IRequestHandler<CreateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var name = request.Event.Name.Trim();
        var slug = GenerateSlug(name);

        await EnsureSlugIsUnique(slug);

        var ev = CreateEventEntity(request, name, slug);

        await eventRepository.AddEvent(ev);

        logger.LogInformation("Event {EventId} created with slug {Slug}", ev.Id, ev.Slug);

        return MapToDto(ev);
    }

    /* ===================== PRIVATE HELPERS ===================== */

    private async Task EnsureSlugIsUnique(string slug)
    {
        var exists = await eventRepository.GetEventBySlug(slug);
        if (exists)
        {
            logger.LogWarning("Attempt to create event with duplicate slug {Slug}", slug);

            throw new InvalidOperationException($"Slug already in use: {slug}");
        }
    }

    private static Event CreateEventEntity(CreateEventCommand request, string name, string slug)
    {
        return new Event
        {
            Name = name,
            Slug = slug,
            Type = ParseEventType(request.Event.Type),
            StartsAtUtc = request.Event.StartDate,
            EndsAtUtc = request.Event.EndDate,
            DefaultTargetWords = request.Event.DefaultTargetWords,
            IsActive = true
        };
    }

    private static EventType ParseEventType(string type) 
        => Enum.TryParse<EventType>(type, true, out var parsed) ? parsed : EventType.Nanowrimo;

    private static EventDto MapToDto(Event ev)
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
