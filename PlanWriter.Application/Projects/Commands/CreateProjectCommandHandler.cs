using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Projects.Commands;

public class CreateProjectCommandHandler(
    ILogger<CreateProjectCommandHandler> logger,
    IProjectRepository projectRepository)
    : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (request.Project is null)
            throw new ArgumentNullException(nameof(request.Project));

        logger.LogInformation("Creating project. UserId={UserId} Title={Title}", request.UserId, request.Project.Title);

        var projectEntity = CreateProjectEntity(request);

        await projectRepository.CreateAsync(projectEntity , cancellationToken);

        logger.LogInformation("Project created. ProjectId={ProjectId} UserId={UserId}", projectEntity.Id, request.UserId);

        var projectDto = MapToDto(projectEntity);

        logger.LogInformation("Returning created project DTO. ProjectId={ProjectId} GoalTarget={GoalTarget} ProgressPercent={ProgressPercent}",
            projectDto.Id, projectDto.WordCountGoal, projectDto.ProgressPercent);

        return projectDto;
    }
    
    private static Project CreateProjectEntity(CreateProjectCommand request)
    {
        var nowUtc = DateTime.UtcNow;

        var title = request.Project.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Title is required.");

        var description = request.Project.Description?.Trim();

        var wordCountGoal = request.Project.WordCountGoal;
        var goalAmount = wordCountGoal ?? 0;

        var startDateUtc = request.Project.StartDate ?? nowUtc;

        return new Project
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,

            Title = title,
            Description = description,
            Genre = request.Project.Genre,

            WordCountGoal = wordCountGoal,
            GoalAmount = goalAmount,
            GoalUnit = GoalUnit.Words,

            StartDate = startDateUtc,
            Deadline = request.Project.Deadline,

            CreatedAt = nowUtc,
            CurrentWordCount = 0
        };
    }

    private static ProjectDto MapToDto(Project project)
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
            HasCover = project.CoverBytes is { Length: > 0 },
            CoverUpdatedAt = project.CoverUpdatedAt,
            StartDate = project.StartDate
        };
    }

    private static int? ResolveGoalTarget(Project project)
    {
        if (project.WordCountGoal.HasValue && project.WordCountGoal.Value > 0)
            return project.WordCountGoal.Value;

        return project.GoalAmount > 0 ? project.GoalAmount : null;
    }
}
