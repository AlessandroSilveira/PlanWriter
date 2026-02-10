using System;
using MediatR;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Application.AdminEvents.Dtos.Queries;

public class GetAdminEventByIdQuery(Guid eventId) : IRequest<EventDto?>
{
    public Guid EventId => eventId;
}

    
