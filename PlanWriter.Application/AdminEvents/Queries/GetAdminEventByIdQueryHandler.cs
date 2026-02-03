using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.AdminEvents.Queries;

public class GetAdminEventByIdQueryHandler(IAdminEventReadRepository adminEventReadRepository, ILogger<GetAdminEventByIdQueryHandler> logger) : IRequestHandler<GetAdminEventByIdQuery, EventDto?>
{
    public async Task<EventDto?> Handle(GetAdminEventByIdQuery request, CancellationToken cancellationToken)
    {
        var ev = await adminEventReadRepository.GetByIdAsync(request.EventId, cancellationToken);
        
        if (ev is null)
        {
            logger.LogWarning("Admin Event {EventId} not found", request.EventId);
            return null;
        }
        
        logger.LogInformation("Admin Event {EventId} retrieved successfully", ev.Id);
        return new EventDto(ev.Id, ev.Name, ev.Slug, ev.Type.ToString(),
                ev.StartsAtUtc, ev.EndsAtUtc, ev.DefaultTargetWords, ev.IsActive);
    }
}