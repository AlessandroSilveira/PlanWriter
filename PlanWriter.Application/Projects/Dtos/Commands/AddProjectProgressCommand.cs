using System;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Projects.Dtos.Commands;

public record AddProjectProgressCommand(Guid Id, AddProjectProgressDto Request, Guid UserId) : IRequest<bool>;

    
