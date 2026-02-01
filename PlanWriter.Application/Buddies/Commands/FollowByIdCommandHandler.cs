using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Buddies.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Buddies.Commands;

public class FollowByIdCommandHandler(IUserFollowRepository userFollowRepository, ILogger<FollowByIdCommandHandler> logger)
    : IRequestHandler<FollowByIdCommand, Unit>
{
    public async Task<Unit> Handle(FollowByIdCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {FollowerId} is attempting to follow user {FolloweeId}", request.Me, request.FolloweeId);

        await FollowAsync(request.Me, request.FolloweeId, cancellationToken);

        return Unit.Value;
    }

    private async Task FollowAsync(Guid followerId, Guid followeeId, CancellationToken cancellationToken)
    {
        if (followerId == followeeId)
            throw new InvalidOperationException("Você não pode seguir a si mesmo.");

        var alreadyFollowing = await userFollowRepository
            .ExistsAsync(followerId, followeeId, cancellationToken);

        if (alreadyFollowing)
            return; 

        await userFollowRepository.AddAsync(
            new UserFollow
            {
                FollowerId = followerId,
                FolloweeId = followeeId
            },
            cancellationToken
        );

        logger.LogInformation("User {FollowerId} followed user {FolloweeId}", followerId, followeeId);
    }
}