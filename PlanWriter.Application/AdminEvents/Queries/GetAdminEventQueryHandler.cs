using System.Collections.Generic;
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

public class GetAdminEventQueryHandler(IAdminEventReadRepository adminEventReadRepository, ILogger<GetAdminEventQueryHandler> logger) : IRequestHandler<GetAdminEventsQuery, IReadOnlyList<EventDto>?>
{
    public async Task<IReadOnlyList<EventDto>?> Handle(GetAdminEventsQuery request, CancellationToken cancellationToken)
    {
        var allEvents = await adminEventReadRepository.GetAllAsync(cancellationToken);

        if (allEvents != null && allEvents.Count == 0)
        {
            logger.LogInformation("No events found");
            return [];
        }
        
        logger.LogInformation("Events found");
        return allEvents;


    }
}