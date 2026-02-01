using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Application.Badges.Dtos.Queries;

public class GetByIdQuery(Guid projectId) : IRequest<List<Badge>>
{
    public Guid ProjectId { get; } = projectId;
}