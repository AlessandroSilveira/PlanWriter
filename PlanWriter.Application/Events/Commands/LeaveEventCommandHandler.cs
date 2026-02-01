using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Commands;

public class LeaveEventCommandHandler(IProjectEventsRepository projectEventsRepository,
    ILogger<LeaveEventCommandHandler> logger) : IRequestHandler<LeaveEventCommand, Unit>
{
    public async Task<Unit> Handle(LeaveEventCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Leaving event {EventId} from project {ProjectId}", request.EventId, request.ProjectId);
        await projectEventsRepository.RemoveByKeys(request.ProjectId, request.EventId);
        
        logger.LogInformation("Project {ProjectId} successfully left event {EventId}", request.ProjectId, request.EventId);
        return Unit.Value;
    }
}