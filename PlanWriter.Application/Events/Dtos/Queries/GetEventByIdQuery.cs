using System;
using MediatR;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Application.Events.Dtos.Queries;

public class GetEventByIdQuery(Guid eventId) : IRequest<EventDto?>
{
    public Guid EventId { get; } = eventId;
}