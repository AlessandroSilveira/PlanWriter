using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Infrastructure.ReadModels.Projects;

namespace PlanWriter.Application.Projects.Queries;

public class GetProjectByIdQueryHandler(ILogger<GetProjectByIdQueryHandler> logger, IProjectReadRepository projectRepository)
    : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting project {ProjectId} for user {UserId}", request.Id, request.UserId);

        var project = await projectRepository.GetProjectByIdAsync(request.Id, request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Project not found");

        logger.LogInformation("Access granted to project {ProjectId} for user {UserId}", request.Id, request.UserId
        );

        return MapToDto(project);
    }

    /* ===================== PRIVATE METHODS ===================== */

    private static ProjectDto MapToDto(ProjectDto project)
    {
        var goalTarget = ResolveGoalTarget(project);

        var progressPercent =
            goalTarget.HasValue && goalTarget.Value > 0
                ? (double)project.CurrentWordCount / goalTarget.Value * 100
                : 0;

        return new ProjectDto
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            CurrentWordCount = project.CurrentWordCount,
            WordCountGoal = goalTarget,
            Deadline = project.Deadline,
            ProgressPercent = progressPercent,
            Genre = project.Genre,
            //HasCover = project.CoverBytes?.Length > 0,
            CoverUpdatedAt = project.CoverUpdatedAt,
            StartDate = project.StartDate
        };
    }

    private static int? ResolveGoalTarget(ProjectDto project)
    {
        if (project.WordCountGoal.HasValue && project.WordCountGoal.Value > 0)
            return project.WordCountGoal.Value;

        return project.WordCountGoal > 0
            ? project.WordCountGoal
            : null;
    }
}
