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

public class StartWordWarCommandHandler(ILogger<StartWordWarCommandHandler> logger, 
    IWordWarReadRepository wordWarReadRepository, IWordWarRepository wordWarRepository) : IRequestHandler<StartWordWarCommand, Unit>
{
    public async Task<Unit> Handle(StartWordWarCommand request, CancellationToken cancellationToken)
    {
        var wordwar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);

        if (wordwar is null)
        {
            logger.LogError("WordWar not exist.");
            throw new NotFoundException("WordWar not exist.");
        }

        if (wordwar.Status != WordWarStatus.Waiting)
        {
            logger.LogError("Only word wars in waiting status can be started.");
            throw new BusinessRuleException("Only word wars in waiting status can be started.");
        }

        if (wordwar.DurationInMinuts <= 0)
        {
            logger.LogError("WordWar has invalid duration.");
            throw new BusinessRuleException("WordWar has invalid duration.");
        }

        var startsAtUtc = DateTime.UtcNow;
        var endsAtUtc = startsAtUtc.AddMinutes(wordwar.DurationInMinuts);

        var response = await wordWarRepository.StartAsync(wordwar.Id, startsAtUtc, endsAtUtc, cancellationToken);
        if (response == 1)
        {
            logger.LogInformation(
                "Word war state changed to Running. WarId: {WarId}, RequestedByUserId: {RequestedByUserId}, StartsAtUtc: {StartsAtUtc}, EndsAtUtc: {EndsAtUtc}",
                request.WarId,
                request.RequestedByUserId,
                startsAtUtc,
                endsAtUtc);
            return Unit.Value;
        }

        var latestWordWar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);
        if (latestWordWar?.Status == WordWarStatus.Running)
        {
            logger.LogInformation(
                "Word war start treated as idempotent due to concurrent start. WarId: {WarId}, RequestedByUserId: {RequestedByUserId}",
                request.WarId,
                request.RequestedByUserId);
            return Unit.Value;
        }

        logger.LogError("A state conflict occurred while attempting to start the word war.");
        throw new BusinessRuleException("A state conflict occurred while attempting to start the word war.");
    }
}
