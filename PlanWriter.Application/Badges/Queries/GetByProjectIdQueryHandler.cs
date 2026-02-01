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

public class GetByProjectIdQueryHandler(ILogger<GetByProjectIdQueryHandler> logger, IBadgeRepository badgeRepository) : IRequestHandler<GetByProjectIdQuery, List<Badge>>
{
    public async Task<List<Badge>> Handle(GetByProjectIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting badges for project {ProjectId}", request.ProjectId);
        var listBadges = await badgeRepository.GetByProjectIdAsync(request.ProjectId);
        
        logger.LogInformation("Found {Count} badges for project {ProjectId}", listBadges.Count(), request.ProjectId);
        return listBadges.ToList();
    }
}