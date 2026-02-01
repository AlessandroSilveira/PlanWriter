using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Buddies.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Buddies.Commands;

public class FollowByUsernameCommandHandler(
    IBuddiesRepository buddiesRepository,
    IUserFollowRepository userFollowRepository,
    ILogger<FollowByUsernameCommandHandler> logger)
    : IRequestHandler<FollowByUsernameCommand, Unit>
{
    public async Task<Unit> Handle(
        FollowByUsernameCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("User {FollowerId} is attempting to follow {Username}", request.Me, request.ReqEmail);

        var followeeId = await buddiesRepository
                             .FindUserIdByUsernameAsync(request.ReqEmail, cancellationToken)
                         ?? throw new KeyNotFoundException("Usuário não encontrado.");

        await FollowAsync(request.Me, followeeId, cancellationToken);

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

        logger.LogInformation("User {FollowerId} is now following user {FolloweeId}", followerId, followeeId);
    }
}