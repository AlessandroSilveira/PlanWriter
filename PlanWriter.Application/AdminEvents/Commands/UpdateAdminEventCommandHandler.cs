using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories.Events.Admin;

namespace PlanWriter.Application.AdminEvents.Commands;

public class UpdateAdminEventCommandHandler(IAdminEventReadRepository adminEventReadRepository, IAdminEventRepository adminEventRepository, ILogger<UpdateAdminEventCommandHandler> logger) : IRequestHandler<UpdateAdminEventCommand, Unit>
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
                IsActive = request.Request.IsActive
            };

            await adminEventRepository.UpdateAsync(request.Id, updatedEvent, cancellationToken);

            logger.LogInformation("Event {EventId} updated", request.Id);
            return Unit.Value;
        }
        catch (Exception e)
        {
           logger.LogError(e, "Error updating event {EventId}", request.Id);
           throw new Exception("Error updating event");
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
}
