using System;
using MediatR;

namespace PlanWriter.Application.WordWar.Dtos.Commands;

public record CreateWordWarCommand(Guid EventId, int DurationMinutes, Guid RequestedByUserId ) : IRequest<Guid>;