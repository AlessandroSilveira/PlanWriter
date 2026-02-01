using System;
using MediatR;

namespace PlanWriter.Application.Projects.Dtos.Commands;

public record DeleteProjectCommand(Guid ProjectId, Guid UserId) : IRequest<bool>;