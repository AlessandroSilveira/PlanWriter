using System;
using MediatR;

namespace PlanWriter.Application.WordWar.Dtos.Commands;

public record FinishWordWarCommand(Guid WarId, Guid RequestedByUserId) : IRequest<Unit>;
