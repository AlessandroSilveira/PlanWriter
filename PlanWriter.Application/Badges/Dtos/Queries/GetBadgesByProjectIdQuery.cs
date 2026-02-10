using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos.Badges;

namespace PlanWriter.Application.Badges.Dtos.Queries;

public record GetBadgesByProjectIdQuery(Guid ProjectId, Guid UserId) : IRequest<IReadOnlyList<BadgeDto>>
{
    
}
