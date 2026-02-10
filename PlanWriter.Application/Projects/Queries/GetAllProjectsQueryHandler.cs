using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;

namespace PlanWriter.Application.Projects.Queries;

public class GetAllProjectsQueryHandler(ILogger<GetAllProjectsQueryHandler> logger, IProjectReadRepository projectRepository)
    : IRequestHandler<GetAllProjectsQuery, List<ProjectDto>>
{
    public async Task<List<ProjectDto>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting projects for user {UserId}", request.UserId);

        var userProjects = await projectRepository.GetUserProjectsAsync(request.UserId, cancellationToken);

        var enumerable = userProjects.ToList();
        logger.LogInformation("Found {Count} projects for user {UserId}", enumerable.Count, request.UserId);

        return enumerable.Select(MapToDto)
            .ToList();
    }

    private static ProjectDto MapToDto(ProjectDto project)
    {
        var goalTarget = ResolveGoalTarget(project);

        var progressPercent = goalTarget.HasValue && goalTarget.Value > 0 ? (double)project.CurrentWordCount / goalTarget.Value * 100 : 0;

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
            ///HasCover = project.CoverBytes?.Length > 0,
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
