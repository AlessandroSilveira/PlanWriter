using System;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Projects.Dtos.Commands;

public record SetGoalProjectCommand(Guid ProjectId, Guid UserId, SetFlexibleGoalDto Request) : IRequest<bool>;