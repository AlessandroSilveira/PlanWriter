using System;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Projects.Dtos.Queries;

public record GetProjectByIdQuery(Guid Id, Guid UserId) : IRequest<ProjectDto>;

   
