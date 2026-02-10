using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos.Buddies;

namespace PlanWriter.Application.Buddies.Dtos.Queries;

public class GetListBuddiesQuery(Guid me) : IRequest<List<BuddiesDto.BuddySummaryDto>>
{
    public Guid Me { get; } = me;
}