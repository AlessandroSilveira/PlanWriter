using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Repositories.Events.Admin;

namespace PlanWriter.Application.AdminEvents.Commands;

public class DeleteAdminEventCommandHandler(IAdminEventReadRepository adminEventReadRepository, IAdminEventRepository adminEventRepository, ILogger<DeleteAdminEventCommandHandler> logger) : IRequestHandler<DeleteAdminEventCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAdminEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await adminEventReadRepository.GetByIdAsync(request.Id, cancellationToken);

        if (ev is null)
        {
            logger.LogWarning("Event {EventId} not found", request.Id);
            throw new InvalidOperationException("Event  not found");
        } 

        await adminEventRepository.DeleteAsync(ev, cancellationToken);
        
        logger.LogInformation("Event {EventId} deleted", request.Id);
        return Unit.Value;
    }
}