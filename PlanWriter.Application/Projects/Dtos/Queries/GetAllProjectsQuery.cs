using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Application.Projects.Dtos.Queries;

public record GetAllProjectsQuery(Guid UserId) : IRequest<List<ProjectDto>>;