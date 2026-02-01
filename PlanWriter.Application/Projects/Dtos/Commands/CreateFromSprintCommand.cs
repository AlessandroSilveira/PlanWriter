using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Projects.Dtos.Commands;

public record CreateFromSprintCommand(CreateSprintProgressDto CreateSprint) : IRequest<Unit>;

    
