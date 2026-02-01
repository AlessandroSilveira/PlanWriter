using System.Security.Claims;
using MediatR;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Application.DailyWordLogs.Dtos.Commands;

public class UpsertDailyWordLogCommand(CreateDailyWordLogRequest req, ClaimsPrincipal user) : IRequest<Unit>
{
    public CreateDailyWordLogRequest Req { get; } = req;
    public ClaimsPrincipal User { get; } = user;
}