using System;
using MediatR;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Application.Projects.Dtos.Queries;

public record GetProjectByIdQuery(Guid Id, Guid UserId) : IRequest<ProjectDto>;

   
