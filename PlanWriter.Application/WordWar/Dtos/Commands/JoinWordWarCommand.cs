using System;
using MediatR;

namespace PlanWriter.Application.WordWar.Dtos.Commands;

public record JoinWordWarCommand(Guid WarId, Guid UserId, Guid ProjectId) : IRequest<bool>;
