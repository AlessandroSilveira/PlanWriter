using System;
using MediatR;

namespace PlanWriter.Application.Events.Dtos.Commands;

public class LeaveEventCommand(Guid projectId, Guid eventId) : IRequest<Unit>
{
    public Guid ProjectId { get; } = projectId;
    public Guid EventId { get; } = eventId;
}