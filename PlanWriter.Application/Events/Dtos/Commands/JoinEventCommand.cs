using System;
using MediatR;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;

namespace PlanWriter.Application.Events.Dtos.Commands;

public record JoinEventCommand(JoinEventRequest Req, Guid UserId) : IRequest<ProjectEvent>;
