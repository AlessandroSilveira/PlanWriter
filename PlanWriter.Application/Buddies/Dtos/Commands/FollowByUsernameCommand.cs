using System;
using MediatR;

namespace PlanWriter.Application.Buddies.Dtos.Commands;

public class FollowByUsernameCommand(Guid me, string reqEmail) : IRequest<Unit>
{
    public Guid Me { get; } = me;
    public string ReqEmail { get; } = reqEmail;
}