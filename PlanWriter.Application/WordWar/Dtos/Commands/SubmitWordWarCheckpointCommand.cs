using System;
using MediatR;

namespace PlanWriter.Application.WordWar.Dtos.Commands;

public record SubmitWordWarCheckpointCommand(Guid WarId, Guid UserId, int WordsInRound) : IRequest<bool>;

    
