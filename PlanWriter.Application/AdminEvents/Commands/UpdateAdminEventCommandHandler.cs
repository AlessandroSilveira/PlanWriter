using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
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

            await adminEventRepository.UpdateAsync(request.Id, ev,  cancellationToken);

            logger.LogInformation("Event {EventId} updated", request.Id);
            return Unit.Value;
        }
        catch (Exception e)
        {
           logger.LogError(e, "Error updating event {EventId}", request.Id);
            throw new Exception("Error updating event");
        }
    }
}