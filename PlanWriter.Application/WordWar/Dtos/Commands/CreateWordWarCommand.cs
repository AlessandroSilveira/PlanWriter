using System;
using MediatR;

namespace PlanWriter.Application.WordWar.Dtos;

public record CreateWordWarCommand(Guid EventId, int DurationMinutes, Guid RequestedByUserId ) : IRequest<Guid>;