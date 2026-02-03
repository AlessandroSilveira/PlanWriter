using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Application.Projects.Dtos.Commands;

namespace PlanWriter.Application.Projects.Commands;

public class DeleteProjectCommandHandler(ILogger<DeleteProjectCommandHandler> logger, IProjectRepository projectRepository)
    : IRequestHandler<DeleteProjectCommand, bool>
{
    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting deletion of project {ProjectId} by user {UserId}", request.ProjectId, request.UserId);

        var deleted = await projectRepository.DeleteProjectAsync(request.ProjectId, request.UserId, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning(
                "Deletion attempt failed: Project {ProjectId} not found or does not belong to user {UserId}",
                request.ProjectId, request.UserId);
        }
        
        logger.LogInformation("Project {ProjectId} successfully deleted by user {UserId}", request.ProjectId, request.UserId);

        return deleted;
    }
}