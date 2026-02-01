using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Events.Dtos.Queries;

public class GetEventLeaderboardQuery(Guid eventId, string scope, int top) : IRequest<List<EventLeaderboardRowDto>>
{
    public Guid EventId { get; } = eventId;
    public string Scope { get; } = scope;
    public int Top { get; } = top;
}