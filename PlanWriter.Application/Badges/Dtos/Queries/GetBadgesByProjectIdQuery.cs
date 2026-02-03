using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos.Badges;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Application.Badges.Dtos.Queries;

public record GetBadgesByProjectIdQuery(Guid ProjectId, Guid UserId) : IRequest<List<BadgeDto>>
{
    
}