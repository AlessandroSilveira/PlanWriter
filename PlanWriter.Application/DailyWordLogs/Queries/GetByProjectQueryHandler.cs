using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.DailyWordLogs.Dtos.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.DailyWordLogWrite;

namespace PlanWriter.Application.DailyWordLogs.Queries;

public class GetByProjectQueryHandler(IDailyWordLogReadRepository readRepository,
    ILogger<GetByProjectQueryHandler> logger
) : IRequestHandler<GetByProjectQuery, IReadOnlyList<DailyWordLogDto>>
{
    public async Task<IReadOnlyList<DailyWordLogDto>> Handle(GetByProjectQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting daily word logs for project {ProjectId} and user {UserId}", request.ProjectId, request.UserId);

        var logs = await readRepository.GetByProjectAsync(request.ProjectId, request.UserId, ct);

        logger.LogInformation("Found {Count} daily word logs for project {ProjectId}", logs.Count, request.ProjectId);
        
        return logs;
    }
}