using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Milestones.Dtos.Queries;

public record GetProjectMilestonesQuery(Guid ProjectId, Guid UserId) : IRequest<List<MilestoneDto>>;