using System;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Milestones.Dtos.Commands;

public record CreateMilestoneCommand(Guid ProjectId, Guid UserId, CreateMilestoneDto Dto) : IRequest<MilestoneDto>;