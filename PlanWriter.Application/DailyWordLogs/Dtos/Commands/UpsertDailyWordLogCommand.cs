using System;
using MediatR;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Application.DailyWordLogs.Dtos.Commands;

public record UpsertDailyWordLogCommand(Guid UserId, UpsertDailyWordLogRequest Req) : IRequest<Unit>;