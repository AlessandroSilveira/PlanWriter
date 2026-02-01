using System;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Projects.Dtos.Commands;

public record CreateProjectCommand(CreateProjectDto Project, Guid UserId) : IRequest<ProjectDto>;
