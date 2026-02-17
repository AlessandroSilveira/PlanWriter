using System;
using MediatR;
using PlanWriter.Domain.Dtos.Goodies;

namespace PlanWriter.Application.Goodies.Dtos.Queries;

public sealed record GetEventGoodiesQuery(Guid UserId, Guid EventId, Guid ProjectId)
    : IRequest<EventGoodiesDto>;
