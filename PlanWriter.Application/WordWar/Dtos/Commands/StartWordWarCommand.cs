using System;
using MediatR;

namespace PlanWriter.Application.WordWar.Dtos.Commands;

public record StartWordWarCommand(Guid WarId, Guid RequestedByUserId) : IRequest<Unit>;

    
