using System;
using MediatR;

namespace PlanWriter.Application.EventValidation.Dtos.Queries;

public sealed record ValidationStatusQuery(Guid CurrentUserId, Guid EventId, Guid ProjectId)
    : IRequest<ValidationStatusDto>;
