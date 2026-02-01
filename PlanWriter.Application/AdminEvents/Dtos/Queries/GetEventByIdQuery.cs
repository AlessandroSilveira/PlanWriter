using System;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.AdminEvents.Dtos.Queries;

public class GetEventByIdQuery(Guid eventId) : IRequest<EventDto?>
{
    public Guid EventId => eventId;
}

    
