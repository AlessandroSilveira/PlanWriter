using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Application.Events.Dtos.Queries;

public class GetMyEventsQuery(Guid userId) : IRequest<List<MyEventDto>>
{
    public Guid UserId { get; } = userId;
}