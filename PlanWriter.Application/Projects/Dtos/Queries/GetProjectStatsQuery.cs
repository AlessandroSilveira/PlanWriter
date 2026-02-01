using System;
using System.Security.Claims;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Projects.Dtos.Queries;

public record GetProjectStatsQuery(Guid ProjectId, Guid UserId) : IRequest<ProjectStatsDto>;

    
