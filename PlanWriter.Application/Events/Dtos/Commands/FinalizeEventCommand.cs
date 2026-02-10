using MediatR;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;

namespace PlanWriter.Application.Events.Dtos.Commands;

public class FinalizeEventCommand(FinalizeRequest req) : IRequest<ProjectEvent>
{
    public FinalizeRequest Req { get; } = req;
}