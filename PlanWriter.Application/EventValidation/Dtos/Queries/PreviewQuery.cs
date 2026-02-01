using System;
using MediatR;

namespace PlanWriter.Application.EventValidation.Dtos.Queries;

public record PreviewQuery(Guid CurrentUserId, Guid EventId, Guid ProjectId) 
    : IRequest<(int target, int total)>;