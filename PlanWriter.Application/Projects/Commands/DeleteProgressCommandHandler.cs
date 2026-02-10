using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Projects.Commands;

public class DeleteProgressCommandHandler(ILogger<DeleteProgressCommandHandler> logger, IProjectProgressReadRepository progressReadRepository,   
    IProjectProgressRepository progressWriteRepository, IProjectRepository projectRepository, IProjectReadRepository projectReadRepository)                   
    : IRequestHandler<DeleteProgressCommand, bool>
{
    public async Task<bool> Handle(DeleteProgressCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting progress {ProgressId} for user {UserId}", request.ProgressId, request.UserId);

        var progressRow = await progressReadRepository.GetByIdAsync(request.ProgressId, request.UserId, cancellationToken);
        if (progressRow is null)
        {
            logger.LogWarning("Progress {ProgressId} not found or not owned by user {UserId}", request.ProgressId, request.UserId);
            return false;
        }

        var projectId = progressRow.ProjectId;
        var progressDate = progressRow.Date;

        var deleted = await progressWriteRepository.DeleteAsync(request.ProgressId, request.UserId);
        if (!deleted)
        {
            logger.LogWarning("Progress {ProgressId} could not be deleted for user {UserId}", request.ProgressId, request.UserId);
            return false;
        }

        var lastTotal = await progressReadRepository.GetLastTotalBeforeAsync(projectId, request.UserId, progressDate, cancellationToken);

        var project = await projectReadRepository.GetUserProjectByIdAsync(projectId, request.UserId, cancellationToken);
        if (project is null)
        {
            logger.LogWarning("Project {ProjectId} not found for user {UserId} during recalculation", projectId, request.UserId);
            return false;
        }

        project.CurrentWordCount = lastTotal;
        await projectRepository.UpdateAsync(project, cancellationToken);

        logger.LogInformation("Progress {ProgressId} deleted; project {ProjectId} recalculated to {Total}", request.ProgressId, projectId, lastTotal);

        return true;
    }
}
