using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.DailyWordLogs.Dtos.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.DailyWordLogs.Queries;

public class GetByProjectQueryHandler(IDailyWordLogRepository dailyWordLogRepository, ILogger<GetByProjectQueryHandler> logger)
    : IRequestHandler<GetByProjectQuery, List<DailyWordLogDto>>
{
    public async Task<List<DailyWordLogDto>> Handle(GetByProjectQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Getting daily word logs for project {ProjectId} and user {UserId}",
            request.ProjectId,
            request.UserId
        );

        var logs = await dailyWordLogRepository
            .GetByProjectAsync(request.ProjectId, request.UserId);

        logger.LogInformation(
            "Found {Count} daily word logs for project {ProjectId}",
            logs.Count(),
            request.ProjectId
        );

        return MapToDto(logs);
    }

    private static List<DailyWordLogDto> MapToDto(IEnumerable<Domain.Entities.DailyWordLog> logs)
    {
        return logs
            .Select(x => new DailyWordLogDto
            {
                Date = x.Date,
                WordsWritten = x.WordsWritten
            })
            .ToList();
    }
}