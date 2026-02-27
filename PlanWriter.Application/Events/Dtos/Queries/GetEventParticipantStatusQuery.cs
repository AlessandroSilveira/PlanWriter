using System;
using MediatR;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Application.Events.Dtos.Queries;

public sealed record GetEventParticipantStatusQuery(Guid UserId, Guid EventId, Guid ProjectId)
    : IRequest<EventParticipantStatusDto>;
