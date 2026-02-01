using MediatR;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Events;

namespace PlanWriter.Application.Events.Dtos.Commands;

public class JoinEventCommand(JoinEventRequest req) : IRequest<ProjectEvent>
{
    public JoinEventRequest Req { get; } = req;
}