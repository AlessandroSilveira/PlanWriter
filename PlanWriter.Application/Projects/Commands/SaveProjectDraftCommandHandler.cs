using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Projects.Commands;

public class SaveProjectDraftCommandHandler(
    ILogger<SaveProjectDraftCommandHandler> logger,
    IProjectReadRepository projectReadRepository,
    IProjectDraftRepository projectDraftRepository)
    : IRequestHandler<SaveProjectDraftCommand, ProjectDraftDto>
{
    public async Task<ProjectDraftDto> Handle(SaveProjectDraftCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Draft);

        var project = await projectReadRepository.GetUserProjectByIdAsync(request.ProjectId, request.UserId, cancellationToken);
        if (project is null)
            throw new NotFoundException("Project not found.");

        var updatedAtUtc = DateTime.UtcNow;

        logger.LogInformation(
            "Saving rich draft for project {ProjectId} and user {UserId}",
            request.ProjectId,
            request.UserId);

        return await projectDraftRepository.UpsertAsync(
            request.ProjectId,
            request.UserId,
            request.Draft.HtmlContent ?? string.Empty,
            updatedAtUtc,
            cancellationToken);
    }
}
