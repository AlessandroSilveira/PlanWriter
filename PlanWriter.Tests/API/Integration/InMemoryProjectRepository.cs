using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Tests.API.Integration;

public sealed class InMemoryProjectRepository(InMemoryProfileStore store)
    : IProjectRepository, IProjectReadRepository
{
    public Task<Project> CreateAsync(Project project, CancellationToken ct)
    {
        if (project.Id == Guid.Empty)
        {
            project.Id = Guid.NewGuid();
        }

        store.UpsertProject(project);
        return Task.FromResult(project);
    }

    public Task<IReadOnlyList<Project>> GetUserProjectsAsync(Guid userId, CancellationToken ct)
    {
        return Task.FromResult(store.GetProjectsByUser(userId));
    }

    public Task<Project?> GetProjectWithProgressAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var project = store.GetProjectById(id);
        return Task.FromResult(project is { UserId: var owner } && owner == userId ? project : null);
    }

    public Task<bool> SetGoalAsync(
        Guid projectId,
        Guid userId,
        int wordCountGoal,
        DateTime? deadline = null,
        CancellationToken ct = default)
    {
        var project = store.GetProjectById(projectId);
        if (project is null || project.UserId != userId)
        {
            return Task.FromResult(false);
        }

        project.WordCountGoal = wordCountGoal;
        project.GoalAmount = wordCountGoal;
        project.GoalUnit = GoalUnit.Words;
        project.Deadline = deadline;
        store.UpsertProject(project);
        return Task.FromResult(true);
    }

    public Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, Guid userId)
    {
        var project = store.GetProjectById(projectId);
        if (project is null || project.UserId != userId)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        var goal = project.WordCountGoal ?? 0;
        var remaining = Math.Max(0, goal - project.CurrentWordCount);

        return Task.FromResult(new ProjectStatisticsDto
        {
            TotalWordsWritten = project.CurrentWordCount,
            WordCountGoal = project.WordCountGoal,
            RemainingWords = remaining,
            ProgressPercentage = goal == 0 ? 0 : Math.Min(100.0, (double)project.CurrentWordCount / goal * 100.0),
            AverageWordsPerDay = 0,
            TotalTimeSpentInMinutes = 0,
            Deadline = project.Deadline,
            DaysRemaining = project.Deadline.HasValue
                ? Math.Max(0, (project.Deadline.Value.Date - DateTime.UtcNow.Date).Days)
                : 0
        });
    }

    public Task<bool> DeleteProjectAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        return Task.FromResult(store.DeleteProject(projectId, userId));
    }

    public Task<Project?> GetProjectById(Guid id)
    {
        return Task.FromResult(store.GetProjectById(id));
    }

    public Task ApplyValidationAsync(Guid projectId, ValidationResultDto res, CancellationToken ct)
    {
        var project = store.GetProjectById(projectId);
        if (project is null)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        project.ValidatedWords = res.Words;
        project.ValidatedAtUtc = res.ValidatedAtUtc;
        project.ValidationPassed = res.MeetsGoal;
        store.UpsertProject(project);
        return Task.CompletedTask;
    }

    public Task<(int? goalWords, string? title)> GetGoalAndTitleAsync(Guid projectId, CancellationToken ct)
    {
        var project = store.GetProjectById(projectId);
        if (project is null)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        return Task.FromResult((project.WordCountGoal, project.Title));
    }

    public Task SaveValidationAsync(Guid projectId, int words, bool passed, DateTime utcNow, CancellationToken ct)
    {
        var project = store.GetProjectById(projectId);
        if (project is null)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        project.ValidatedWords = words;
        project.ValidationPassed = passed;
        project.ValidatedAtUtc = utcNow;
        store.UpsertProject(project);
        return Task.CompletedTask;
    }

    public Task<(int goalAmount, GoalUnit unit)> GetGoalAsync(Guid projectId, CancellationToken ct)
    {
        var project = store.GetProjectById(projectId);
        if (project is null)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        return Task.FromResult((project.GoalAmount, project.GoalUnit));
    }

    public Task<bool> UserOwnsProjectAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        var project = store.GetProjectById(projectId);
        return Task.FromResult(project is { UserId: var owner } && owner == userId);
    }

    public Task UpdateFlexibleGoalAsync(
        Guid projectId,
        int goalAmount,
        GoalUnit unit,
        DateTime? deadline,
        CancellationToken ct)
    {
        var project = store.GetProjectById(projectId)
            ?? throw new KeyNotFoundException("Project not found.");

        project.GoalAmount = goalAmount;
        project.GoalUnit = unit;
        project.Deadline = deadline;
        if (unit == GoalUnit.Words)
        {
            project.WordCountGoal = goalAmount;
        }

        store.UpsertProject(project);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProjectDto>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var projects = store.GetProjectsByUser(userId).Select(MapToDto).ToList();
        return Task.FromResult((IReadOnlyList<ProjectDto>)projects);
    }

    public Task<IReadOnlyList<Project>> GetPublicProjectsByUserIdAsync(Guid userId)
    {
        return Task.FromResult(store.GetPublicProjectsByUser(userId));
    }

    public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult(store.GetAllProjects());
    }

    public Task SetProjectVisibilityAsync(Guid projectId, Guid userId, bool isPublic, CancellationToken ct)
    {
        if (!store.SetProjectVisibility(projectId, userId, isPublic))
        {
            throw new InvalidOperationException("Project not found or does not belong to user.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Project project, CancellationToken ct)
    {
        store.UpsertProject(project);
        return Task.CompletedTask;
    }

    Task<IReadOnlyList<ProjectDto>> IProjectReadRepository.GetUserProjectsAsync(Guid userId, CancellationToken ct)
    {
        var projects = store.GetProjectsByUser(userId).Select(MapToDto).ToList();
        return Task.FromResult((IReadOnlyList<ProjectDto>)projects);
    }

    Task<ProjectDto?> IProjectReadRepository.GetProjectByIdAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        var project = store.GetProjectById(projectId);
        if (project is null || project.UserId != userId)
        {
            return Task.FromResult<ProjectDto?>(null);
        }

        return Task.FromResult<ProjectDto?>(MapToDto(project));
    }

    Task<Project?> IProjectReadRepository.GetUserProjectByIdAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        var project = store.GetProjectById(projectId);
        if (project is null || project.UserId != userId)
        {
            return Task.FromResult<Project?>(null);
        }

        return Task.FromResult<Project?>(project);
    }

    private static ProjectDto MapToDto(Project project)
    {
        var goal = project.WordCountGoal ?? project.GoalAmount;
        var progressPercent = goal <= 0 ? 0 : Math.Min(100.0, (double)project.CurrentWordCount / goal * 100.0);

        return new ProjectDto
        {
            Id = project.Id,
            Title = project.Title ?? string.Empty,
            Description = project.Description ?? string.Empty,
            CurrentWordCount = project.CurrentWordCount,
            WordCountGoal = project.WordCountGoal,
            Deadline = project.Deadline,
            ProgressPercent = progressPercent,
            Genre = project.Genre,
            HasCover = project.CoverBytes is { Length: > 0 },
            CoverUpdatedAt = project.CoverUpdatedAt,
            StartDate = project.StartDate,
            GoalUnit = project.GoalUnit,
            IsPublic = project.IsPublic
        };
    }
}
