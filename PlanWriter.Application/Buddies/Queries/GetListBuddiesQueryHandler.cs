using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Buddies.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Buddies;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Buddies.Queries;

public class GetListBuddiesQueryHandler(IBuddiesRepository buddiesRepository, IUserFollowRepository userFollowRepository ,ILogger<GetListBuddiesQueryHandler> logger) 
    : IRequestHandler<GetListBuddiesQuery, List<BuddiesDto.BuddySummaryDto>>
{
    public async Task<List<BuddiesDto.BuddySummaryDto>> Handle(GetListBuddiesQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting list of buddies for user {Me}", request.Me);
        var ids = await userFollowRepository.GetFolloweeIdsAsync(request.Me, cancellationToken);
        
        logger.LogInformation("Found {Count} buddies", ids.Count);
        return await buddiesRepository.GetBuddySummariesAsync(ids, cancellationToken);
    }
}