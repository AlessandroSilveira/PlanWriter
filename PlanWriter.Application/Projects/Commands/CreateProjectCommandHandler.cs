using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Projects.Commands;

public sealed class CreateProjectCommandHandler(ILogger<CreateProjectCommandHandler> logger, IProjectRepository projectRepository)
    : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (request.Project is null)
            throw new ArgumentNullException(nameof(request.Project));

        var userId = request.UserId;
        var rawTitle = request.Project.Title;
        var normalizedTitle = rawTitle?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedTitle))
            throw new InvalidOperationException("Title is required.");

        logger.LogInformation(
            "Creating project. UserId={UserId} Title={Title}",
            userId,
            normalizedTitle
        );

        var nowUtc = DateTime.UtcNow;

        var projectEntity = new Project
        {
            Id = Guid.NewGuid(),
            UserId = userId,

            Title = normalizedTitle,
            Description = request.Project.Description?.Trim(),
            Genre = request.Project.Genre,

            WordCountGoal = request.Project.WordCountGoal,
            GoalAmount = request.Project.WordCountGoal ?? 0,
            GoalUnit = GoalUnit.Words,

            StartDate = request.Project.StartDate ?? nowUtc,
            Deadline = request.Project.Deadline,

            CreatedAt = nowUtc,
            CurrentWordCount = 0
        };

        await projectRepository.CreateAsync(projectEntity, cancellationToken);

        logger.LogInformation("Project created. ProjectId={ProjectId} UserId={UserId}", projectEntity.Id, userId);

        return MapToDto(projectEntity);
    }

    private static ProjectDto MapToDto(Project project)
    {
        var goalTarget = ResolveGoalTarget(project);
        var progressPercent = goalTarget is > 0
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
            HasCover = project.CoverBytes is { Length: > 0 },
            CoverUpdatedAt = project.CoverUpdatedAt,
            StartDate = project.StartDate
        };
    }

    private static int? ResolveGoalTarget(Project project)
        => project.WordCountGoal is > 0
            ? project.WordCountGoal
            : (project.GoalAmount > 0 ? project.GoalAmount : null);
}
