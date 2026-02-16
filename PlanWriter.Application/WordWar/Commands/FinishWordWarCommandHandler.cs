using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Dtos.Commands;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;

namespace PlanWriter.Application.WordWar.Commands;

public class FinishWordWarCommandHandler(ILogger<FinishWordWarCommandHandler> logger,
    IWordWarReadRepository wordWarReadRepository,
    IWordWarRepository wordWarRepository) : IRequestHandler<FinishWordWarCommand, Unit>
{
    public async Task<Unit> Handle(FinishWordWarCommand request, CancellationToken cancellationToken)
    {
        var wordWar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);

        if (wordWar is null)
        {
            logger.LogError("WordWar not exist.");
            throw new NotFoundException("WordWar not exist.");
        }
        
        if (wordWar.Status != WordWarStatus.Running)
        {
            logger.LogError("Only word wars in running status can be finished.");
            throw new BusinessRuleException("Only word wars in running status can be finished.");
        }

        var affected = await wordWarRepository.FinishAsync(request.WarId, DateTime.UtcNow, cancellationToken);
        if (affected == 0)
            throw new BusinessRuleException("Word War não está em execução.");

        await wordWarRepository.PersistFinalRankAsync(request.WarId, cancellationToken);

        return Unit.Value;

    }
}
