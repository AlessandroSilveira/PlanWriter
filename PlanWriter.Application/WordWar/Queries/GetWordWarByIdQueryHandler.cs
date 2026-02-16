using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Dtos.Queries;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;

namespace PlanWriter.Application.WordWar.Queries;

public class GetWordWarByIdQueryHandler(ILogger<GetWordWarByIdQueryHandler> logger,
    IWordWarReadRepository wordWarReadRepository)  : IRequestHandler<GetWordWarByIdQuery , WordWarDto>
{
    public async Task<WordWarDto> Handle(GetWordWarByIdQuery request, CancellationToken cancellationToken)
    {
        var wordwar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);

        if (wordwar is null)
        {
            logger.LogError("Word war not found");
            throw new NotFoundException("Word war not found");
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