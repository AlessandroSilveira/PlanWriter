using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Projects.Commands;

public class SetGoalProjectCommandHandler(ILogger<SetGoalProjectCommandHandler> logger, IProjectRepository projectRepository)
    : IRequestHandler<SetGoalProjectCommand, bool>
{
    public async Task<bool> Handle(SetGoalProjectCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Setting goal for project {ProjectId} by user {UserId}", request.ProjectId, request.UserId);

        var goalAmount = request.Request.GoalAmount;
        var deadline = request.Request.Deadline;

        var result = await projectRepository.SetGoalAsync(request.ProjectId, request.UserId, goalAmount, deadline);

        logger.LogInformation("Goal set result for project {ProjectId}: {Result}", request.ProjectId, result);

        return result;
    }
}