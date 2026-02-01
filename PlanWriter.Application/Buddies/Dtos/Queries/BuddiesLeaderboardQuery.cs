using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos.Buddies;

namespace PlanWriter.Application.Buddies.Dtos.Queries;

public class BuddiesLeaderboardQuery(Guid userId, DateTime? start, DateTime? end) : IRequest<List<BuddyLeaderboardRowDto>>
{
    public Guid UserId { get; } = userId;
    public DateTime? Start { get; } = start;
    public DateTime? End { get; } = end;
}