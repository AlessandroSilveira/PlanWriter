using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Dtos.Commands;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;

namespace PlanWriter.Application.WordWar.Commands;

public class SubmitWordWarCheckpointCommandHandler(ILogger<SubmitWordWarCheckpointCommandHandler> logger,
    IWordWarReadRepository wordWarReadRepository, IWordWarRepository wordWarRepository,
    IWordWarParticipantReadRepository wordWarParticipantReadRepository) : IRequestHandler<SubmitWordWarCheckpointCommand, bool>
{
    public async Task<bool> Handle(SubmitWordWarCheckpointCommand request, CancellationToken cancellationToken)
    {
        if (request.WordsInRound < 0)
        {
            logger.LogError("WordsInRound must be greater than or equal to 0.");
            throw new ValidationException("WordsInRound must be greater than or equal to 0.");
        }
        
        var wordWar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);
        
        if (wordWar is null)
        {
            logger.LogError("WordWar not exist.");
            throw new NotFoundException("WordWar not exist.");
        }

        if (wordWar.Status != WordWarStatus.Running)
        {
            logger.LogError("It's only possible to create a checkpoint when the word war is running.");
            throw new BusinessRuleException("It's only possible to create a checkpoint when the word war is running.");
        }

        var now = DateTime.UtcNow;
        if (now >= wordWar.EndsAtUtc)
        {
            await wordWarRepository.FinishAsync(request.WarId, now, cancellationToken);
            logger.LogError("Word war has been auto-finished by time. Checkpoint rejected.");
            throw new BusinessRuleException("Word war has been auto-finished by time. Checkpoint rejected.");
        }

        var wordWarParticipant =
            await wordWarParticipantReadRepository.GetParticipant(request.WarId, request.UserId, cancellationToken);

        if (wordWarParticipant is null)
        {
            logger.LogError("The user is not participating in this word war.");
            throw new NotFoundException("The user is not participating in this word war.");
        }

        if (request.WordsInRound <= wordWarParticipant.WordsInRound)
        {
            logger.LogError("WordsInRound must be greater than the previous value.");
            throw new BusinessRuleException("WordsInRound must be greater than the previous value.");
        }

        var response = await wordWarRepository.SubmitCheckpointAsync(
            request.WarId,
            request.UserId,
            request.WordsInRound,
            now,
            cancellationToken);

        if (response == 0)
        {
            logger.LogError("Unable to persist checkpoint due to state conflict.");
            throw new BusinessRuleException("Unable to persist checkpoint due to state conflict.");
        }

        return true;

    }
}
