using System;
using MediatR;

namespace PlanWriter.Application.Projects.Dtos.Commands;

public record DeleteProgressCommand(Guid ProgressId, Guid UserId) : IRequest<bool>;

    
