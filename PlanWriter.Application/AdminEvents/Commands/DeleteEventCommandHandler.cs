using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.AdminEvents.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.AdminEvents.Commands;

public class DeleteEventCommandHandler(IEventRepository eventRepository, ILogger<DeleteEventCommandHandler> logger) : IRequestHandler<DeleteEventCommand, Unit>
{
    public async Task<Unit> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await eventRepository.GetEventById(request.Id);

        if (ev is null)
        {
            logger.LogWarning("Event {EventId} not found", request.Id);
            throw new InvalidOperationException("Event  not found");
        } 

        await eventRepository.DeleteAsync(ev);
        
        logger.LogInformation("Event {EventId} deleted", request.Id);
        return Unit.Value;
    }
}