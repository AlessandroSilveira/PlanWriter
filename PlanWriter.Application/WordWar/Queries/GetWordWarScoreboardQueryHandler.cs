using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Domain.Dtos.WordWars;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;

namespace PlanWriter.Application.WordWar.Queries;

public class GetWordWarScoreboardQueryHandler(ILogger<GetWordWarScoreboardQueryHandler> logger,
    IWordWarParticipantReadRepository wordWarParticipantReadRepository,
    IWordWarReadRepository wordWarReadRepository,
    IWordWarRepository wordWarRepository) : IRequestHandler<GetWordWarScoreboardQuery, WordWarScoreboardDto>
{
    public async Task<WordWarScoreboardDto> Handle(GetWordWarScoreboardQuery request, CancellationToken cancellationToken)
    {
        var wordWar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);

        if (wordWar is null)
        {
            logger.LogError("Word war not found.");
            throw new NotFoundException("Word war not found.");
        }

        var now = DateTime.UtcNow;
        if (wordWar.Status == WordWarStatus.Running && now >= wordWar.EndsAtUtc)
        {
            var affected = await wordWarRepository.FinishAsync(request.WarId, now, cancellationToken);
            if (affected > 0)
            {
                await wordWarRepository.PersistFinalRankAsync(request.WarId, cancellationToken);
                wordWar.Status = WordWarStatus.Finished;
                wordWar.FinishedAtUtc = now;
            }
            else
            {
                // Another request may have finished the war concurrently.
                wordWar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);
                if (wordWar is null)
                {
                    logger.LogError("Word war not found after auto-finish attempt.");
                    throw new NotFoundException("Word war not found.");
                }
            }
        }

        var participants = await wordWarParticipantReadRepository.GetScoreboardAsync(request.WarId, cancellationToken);
        var remainingSeconds = Math.Max(0, (int)Math.Ceiling((wordWar.EndsAtUtc - now).TotalSeconds));

        return new WordWarScoreboardDto
        {
            Id = wordWar.Id,
            Status = wordWar.Status,
            DurationMinutes = wordWar.DurationInMinuts,
            RemainingSeconds = remainingSeconds,
            RemainingSecnds = remainingSeconds,
            Participants = participants.ToList()
        };
    }
}
