using System;
using System.Collections.Generic;
using MediatR;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Application.DailyWordLogs.Dtos.Queries;

public class GetByProjectQuery( Guid projectId, Guid userId) : IRequest<List<DailyWordLogDto>>
{
    public Guid UserId { get; } = userId;
    public Guid ProjectId { get; } = projectId;
}