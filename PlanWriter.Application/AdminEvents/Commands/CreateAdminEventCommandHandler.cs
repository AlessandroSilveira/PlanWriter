using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories.Events.Admin;

namespace PlanWriter.Application.AdminEvents.Commands;

public class CreateAdminEventCommandHandler(IAdminEventRepository repository, IAdminEventReadRepository readRepository,
    ILogger<CreateAdminEventCommandHandler> logger) : IRequestHandler<CreateAdminEventCommand, EventDto?>
{
    public async Task<EventDto?> Handle(CreateAdminEventCommand request, CancellationToken ct)
    {
        var name = request.Event.Name.Trim();
        var slug = GenerateSlug(name);

        if (await readRepository.SlugExistsAsync(slug, ct))
        {
            logger.LogWarning("Attempt to create event with duplicate slug {Slug}", slug);

            throw new InvalidOperationException($"Slug already in use: {slug}");
        }

        var ev = CreateEventEntity(request, name, slug);

        await repository.CreateAsync(ev, ct);

        logger.LogInformation("Admin event {EventId} created with slug {Slug}", ev.Id, slug);

        return MapToDto(ev);
    }

    /* ===================== PRIVATE HELPERS ===================== */

    private static Event CreateEventEntity(CreateAdminEventCommand request, string name, string slug
    )
    {
        return new Event
        {
            Id = Guid.NewGuid(),
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
        => Enum.TryParse<EventType>(
            type,
            true,
            out var parsed
        )
            ? parsed
            : EventType.Nanowrimo;

    private static string GenerateSlug(string name)
        => name
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "");
    
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
}

