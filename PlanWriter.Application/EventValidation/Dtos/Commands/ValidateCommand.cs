using System;
using MediatR;

namespace PlanWriter.Application.EventValidation.Dtos.Commands;

public record ValidateCommand(Guid CurrentUserId, Guid EventId, Guid ProjectId, int Words, string Source)
    : IRequest<Unit>;

    
   
