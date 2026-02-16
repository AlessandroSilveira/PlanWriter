using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.WordWar.Dtos.Queries;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;

namespace PlanWriter.Application.WordWar.Queries;

public class GetActiveWordWarByEventIdQueryHandler(ILogger<GetActiveWordWarByEventIdQueryHandler> logger,
    IWordWarReadRepository wordWarReadRepository) : IRequestHandler<GetActiveWordWarByEventIdQuery, WordWarDto?>
{
    public async Task<WordWarDto?> Handle(GetActiveWordWarByEventIdQuery request, CancellationToken cancellationToken)
    {
        var wordwar = await wordWarReadRepository.GetActiveByEventIdAsync(request.EventId, cancellationToken );

        if (wordwar is null)
        {
            return null;
        }

        var wordWarDto = new WordWarDto()
        {
            EndsAtUtc = wordwar.EndsAtUtc,
            FinishedAtUtc = wordwar.FinishedAtUtc,
            Status = wordwar.Status,
            DurationMinutes = wordwar.DurationInMinuts,
            Id = wordwar.Id,
            EventId = wordwar.EventId,
            StartsAtUtc = wordwar.StartsAtUtc
        };

        return wordWarDto;
    }
}