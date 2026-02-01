using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Projects.Commands;

public class DeleteProjectCommandHandler(ILogger<DeleteProjectCommandHandler> logger, IProjectRepository projectRepository) : IRequestHandler<DeleteProjectCommand, bool>
{
    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting project {ProjectId} by user {UserId}", request.ProjectId, request.UserId);
        var deleted = await  projectRepository.DeleteProjectAsync(request.ProjectId, request.UserId);

        logger.LogInformation("Project {ProjectId} deleted by user {UserId}", request.ProjectId, request.UserId);
        return deleted;
    }
}