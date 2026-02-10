using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;

namespace PlanWriter.Application.Projects.Queries;

public class GetProjectProgressHistoryQueryHandler(ILogger<GetProjectProgressHistoryQueryHandler> logger, IProjectProgressReadRepository projectProgressReadRepository)
    : IRequestHandler<GetProjectProgressHistoryQuery, IReadOnlyList<ProgressHistoryDto>>
{
    public async Task<IReadOnlyList<ProgressHistoryDto>> Handle(GetProjectProgressHistoryQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting progress history for project {ProjectId} and user {UserId}", request.ProjectId, request.UserId);

        var rows = await projectProgressReadRepository
            .GetProgressHistoryAsync(request.ProjectId, request.UserId, cancellationToken);

        var result = rows
            .Select(r => new ProgressHistoryDto
            {
                Date = r.Date,
                WordsWritten = r.WordsWritten
            })
            .OrderBy(x => x.Date)
            .ToList();

        logger.LogInformation("Found {Count} progress entries for project {ProjectId} and user {UserId}", result.Count, request.ProjectId, request.UserId);

        return result;
    }
}