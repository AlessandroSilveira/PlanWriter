using System;
using MediatR;

namespace PlanWriter.Application.Buddies.Dtos.Commands;

public class FollowByIdCommand(Guid me, Guid followeeId) : IRequest<Unit>
{
    public Guid Me { get; } = me;
    public Guid FolloweeId { get; } = followeeId;
}