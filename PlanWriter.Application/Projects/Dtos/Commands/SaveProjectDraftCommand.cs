using System;
using MediatR;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Application.Projects.Dtos.Commands;

public record SaveProjectDraftCommand(Guid ProjectId, Guid UserId, SaveProjectDraftDto Draft) : IRequest<ProjectDraftDto>;
