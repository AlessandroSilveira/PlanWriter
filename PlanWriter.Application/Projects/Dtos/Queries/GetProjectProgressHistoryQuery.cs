using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Projects.Dtos.Queries;

public record GetProjectProgressHistoryQuery(Guid ProjectId, Guid UserId)
    : IRequest<IReadOnlyList<ProgressHistoryDto>>;