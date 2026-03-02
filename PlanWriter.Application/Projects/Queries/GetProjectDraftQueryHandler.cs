using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;

namespace PlanWriter.Application.Projects.Queries;

public class GetProjectDraftQueryHandler(
    ILogger<GetProjectDraftQueryHandler> logger,
    IProjectReadRepository projectReadRepository,
    IProjectDraftReadRepository projectDraftReadRepository)
    : IRequestHandler<GetProjectDraftQuery, ProjectDraftDto?>
{
    public async Task<ProjectDraftDto?> Handle(GetProjectDraftQuery request, CancellationToken cancellationToken)
    {
        var project = await projectReadRepository.GetUserProjectByIdAsync(request.ProjectId, request.UserId, cancellationToken);
        if (project is null)
            throw new NotFoundException("Project not found.");

        logger.LogInformation(
            "Getting rich draft for project {ProjectId} and user {UserId}",
            request.ProjectId,
            request.UserId);

        return await projectDraftReadRepository.GetByProjectIdAsync(request.ProjectId, request.UserId, cancellationToken);
    }
}
