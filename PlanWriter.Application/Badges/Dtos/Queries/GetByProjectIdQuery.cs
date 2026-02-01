using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Application.Badges.Dtos.Queries;

public record GetByProjectIdQuery(Guid ProjectId) : IRequest<List<Badge>>;

    
