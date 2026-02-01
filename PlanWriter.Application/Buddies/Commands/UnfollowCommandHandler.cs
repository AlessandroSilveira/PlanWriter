using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Buddies.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Buddies.Commands;

public class UnfollowCommandHandler(IUserFollowRepository userFollowRepository, ILogger<UnfollowCommandHandler> logger)
    : IRequestHandler<UnfollowCommand, Unit>
{
    public async Task<Unit> Handle(UnfollowCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {FollowerId} is attempting to unfollow user {FolloweeId}", request.Me, request.FolloweeId);

        await UnfollowAsync(request.Me, request.FolloweeId, cancellationToken);

        return Unit.Value;
    }

    private async Task UnfollowAsync(Guid followerId, Guid followeeId, CancellationToken cancellationToken)
    {
        await userFollowRepository.RemoveAsync(followerId, followeeId, cancellationToken);
        logger.LogInformation("User {FollowerId} unfollowed user {FolloweeId}", followerId, followeeId);
    }
}