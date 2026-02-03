using System;
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
        ArgumentNullException.ThrowIfNull(request);
        if (request.Request is null) throw new ArgumentNullException(nameof(request.Request));

        logger.LogInformation(
            "Setting goal for project. ProjectId={ProjectId} UserId={UserId} GoalAmount={GoalAmount} Deadline={Deadline}",
            request.ProjectId, request.UserId, request.Request.GoalAmount, request.Request.Deadline);

        Validate(request);

        var updated = await projectRepository.SetGoalAsync(request.ProjectId, request.UserId, request.Request.GoalAmount,
            request.Request.Deadline, cancellationToken);

        if (updated)
        {
            logger.LogInformation("Goal updated successfully. ProjectId={ProjectId} UserId={UserId}",
                request.ProjectId, request.UserId);
        }
        else
        {
            logger.LogWarning("Goal update failed (project not found or not owned by user). ProjectId={ProjectId} UserId={UserId}",
                request.ProjectId, request.UserId);
        }

        return updated;
    }

    private static void Validate(SetGoalProjectCommand request)
    {
        if (request.Request.GoalAmount <= 0)
            throw new InvalidOperationException("GoalAmount must be greater than zero.");

        //opcional (se quiser): não permitir deadline no passado
        if (request.Request.Deadline.HasValue && request.Request.Deadline.Value.Date < DateTime.UtcNow.Date)
            throw new InvalidOperationException("Deadline cannot be in the past.");
    }
}
