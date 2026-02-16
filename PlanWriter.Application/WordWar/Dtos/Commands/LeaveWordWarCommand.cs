using System;
using MediatR;

namespace PlanWriter.Application.WordWar.Dtos.Commands;

public record LeaveWordWarCommand(Guid WarId, Guid UserId) : IRequest<bool>;

    
