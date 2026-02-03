using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Buddies.Dtos.Queries;
using PlanWriter.Domain.Dtos.Buddies;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Buddies.Queries;

public class BuddiesLeaderboardQueryHandler(IUserFollowRepository userFollowRepository, IUserRepository userRepository,
    IProjectProgressRepository projectProgressRepository, ILogger<BuddiesLeaderboardQueryHandler> logger, IProjectProgressReadRepository projectProgressReadRepository
) : IRequestHandler<BuddiesLeaderboardQuery, List<BuddyLeaderboardRowDto>>
{
    public async Task<List<BuddyLeaderboardRowDto>> Handle(BuddiesLeaderboardQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting buddy leaderboard for user {UserId}", request.UserId);

        var (startDate, endDate) = ResolveDateRange(request);

        var buddyIds = await GetBuddyIdsAsync(request.UserId, cancellationToken);

        var totals = await projectProgressReadRepository
            .GetTotalWordsByUsersAsync(buddyIds, startDate, endDate);

        var myTotal = GetTotalForUser(totals, request.UserId);

        var users = await userRepository
            .GetUsersByIdsAsync(buddyIds);

        logger.LogInformation("Found {Count} buddies", buddyIds.Count);

        return BuildLeaderboard(users, totals, myTotal, request.UserId);
    }

    private static (DateTime start, DateTime end) ResolveDateRange(BuddiesLeaderboardQuery request)
        => (request.Start?.Date ?? DateTime.MinValue, request.End?.Date ?? DateTime.MaxValue);

    private async Task<List<Guid>> GetBuddyIdsAsync(Guid me, CancellationToken cancellationToken)
    {
        var ids = await userFollowRepository
            .GetFolloweeIdsAsync(me, cancellationToken);

        if (!ids.Contains(me))
            ids.Add(me);

        return ids;
    }

    private static List<BuddyLeaderboardRowDto> BuildLeaderboard(IEnumerable<Domain.Entities.User> users, IDictionary<Guid, int> totals,
        int myTotal, Guid me)
    {
        return users
            .Select(u =>
            {
                var totalForUser = GetTotalForUser(totals, u.Id);

                return new BuddyLeaderboardRowDto
                {
                    UserId = u.Id,
                    Username = $"{u.FirstName} {u.LastName}",
                    DisplayName = u.DisplayName,
                    Total = totalForUser,
                    PaceDelta = totalForUser - myTotal,
                    IsMe = u.Id == me
                };
            })
            .OrderByDescending(x => x.Total)
            .ToList();
    }

    private static int GetTotalForUser(IDictionary<Guid, int> totals, Guid userId)
        => totals.TryGetValue(userId, out var total) ? total : 0;
}
