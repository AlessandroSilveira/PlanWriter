using System;
using MediatR;

namespace PlanWriter.Application.Milestones.Dtos.Commands;

public record DeleteMilestoneCommand(Guid ProjectId, Guid MilestoneId, Guid UserId) : IRequest<Unit>;
