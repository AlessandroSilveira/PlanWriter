using System;
using MediatR;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Application.Events.Dtos.Queries;

public class GetEventProgressQuery(Guid eventId, Guid projectId) : IRequest<EventProgressDto?>
{
    public Guid EventId { get; } = eventId;
    public Guid ProjectId { get; } = projectId;
}