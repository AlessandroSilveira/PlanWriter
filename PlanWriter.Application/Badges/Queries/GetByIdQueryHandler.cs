using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Badges.Dtos.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Badges.Queries;

public class GetByIdQueryHandler(IBadgeRepository badgeRepository, ILogger<GetByIdQueryHandler> logger) : IRequestHandler<GetByIdQuery, List<Badge>>
{
    public async Task<List<Badge>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting badges for project {ProjectId}", request.ProjectId);
        var badges =  await badgeRepository.GetByProjectIdAsync(request.ProjectId);

        logger.LogInformation("Found {Count} badges for project {ProjectId}", badges.Count(), request.ProjectId);
        return badges.ToList();

    }
}