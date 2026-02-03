using MediatR;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Application.Projects.Dtos.Commands;

public record CreateFromSprintCommand(CreateSprintProgressDto CreateSprint) : IRequest<Unit>;

    
