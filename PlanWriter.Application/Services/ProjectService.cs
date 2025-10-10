using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectProgressRepository _projectProgressRepository;
    private readonly IUserService _userService;

    public ProjectService(
        IProjectRepository projectRepository,
        IProjectProgressRepository projectProgressRepository,
        IUserService userService)
    {
        _projectRepository = projectRepository;
        _projectProgressRepository = projectProgressRepository;
        _userService = userService;
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, ClaimsPrincipal user)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Genre = dto.Genre,
            WordCountGoal = dto.WordCountGoal,
            GoalAmount = dto.WordCountGoal ?? 0,
            GoalUnit = GoalUnit.Words,
            Deadline = dto.Deadline,
            UserId = _userService.GetUserId(user),
            CurrentWordCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _projectRepository.CreateAsync(project);
        return MapToDto(project);
    }

    public async Task<IEnumerable<ProjectDto>> GetUserProjectsAsync(ClaimsPrincipal user)
    {
        var userId = _userService.GetUserId(user);
        var projects = await _projectRepository.GetUserProjectsAsync(userId);

        return projects
            .Where(p => p.UserId == userId)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ProjectDto> GetProjectByIdAsync(Guid id, ClaimsPrincipal user)
    {
        var userId = _userService.GetUserId(user);
        var project = await _projectRepository.GetUserProjectByIdAsync(id, userId)
                      ?? throw new KeyNotFoundException("Project not found");

        return MapToDto(project);
    }

    public async Task AddProgressAsync(AddProjectProgressDto dto, ClaimsPrincipal user)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var userId = _userService.GetUserId(user);
        var project = await _projectRepository.GetUserProjectByIdAsync(dto.ProjectId, userId)
                      ?? throw new KeyNotFoundException("Project not found");

        var words = Math.Max(0, dto.WordsWritten ?? 0);
        var minutes = Math.Max(0, dto.Minutes ?? 0);
        var pages = Math.Max(0, dto.Pages ?? 0);

        if (words <= 0 && minutes <= 0 && pages <= 0)
        {
            throw new ArgumentException("At least one progress value must be greater than zero.", nameof(dto));
        }

        var progressIncrement = project.GoalUnit switch
        {
            GoalUnit.Minutes => minutes,
            GoalUnit.Pages => pages,
            _ => words
        };

        if (progressIncrement <= 0)
        {
            progressIncrement = new[] { words, minutes, pages }.Max();
        }

        var newTotal = project.CurrentWordCount + progressIncrement;
        var target = ResolveGoalTarget(project);

        var progress = new ProjectProgress
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            WordsWritten = words,
            Minutes = minutes,
            Pages = pages,
            TotalWordsWritten = newTotal,
            RemainingWords = target.HasValue ? Math.Max(0, target.Value - newTotal) : 0,
            RemainingPercentage = target.HasValue && target.Value > 0
                ? Math.Round((double)newTotal / target.Value * 100, 2)
                : 0,
            Date = dto.Date == default ? DateTime.UtcNow : dto.Date,
            Notes = dto.Notes
        };

        project.CurrentWordCount = newTotal;

        await _projectProgressRepository.AddProgressAsync(progress);
        await _projectRepository.UpdateAsync(project);
    }

    public async Task<IEnumerable<ProgressHistoryDto>> GetProgressHistoryAsync(Guid projectId, ClaimsPrincipal user)
    {
        var userId = _userService.GetUserId(user);
        var history = await _projectProgressRepository.GetProgressHistoryAsync(projectId, userId);

        return history
            .Select(p => new ProgressHistoryDto
            {
                Date = p.Date,
                WordsWritten = p.WordsWritten
            })
            .OrderBy(p => p.Date)
            .ToList();
    }

    public Task<bool> SetGoalAsync(Guid projectId, string userId, int wordCountGoal, DateTime? deadline)
    {
        return _projectRepository.SetGoalAsync(projectId, userId, wordCountGoal, deadline);
    }

    public Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, string userId)
    {
        return _projectRepository.GetStatisticsAsync(projectId, userId);
    }

    public Task<bool> DeleteProjectAsync(Guid projectId, string userId)
    {
        return _projectRepository.DeleteProjectAsync(projectId, userId);
    }

    public async Task<bool> DeleteProgressAsync(Guid progressId, string userId)
    {
        var progress = await _projectProgressRepository.GetByIdAsync(progressId, userId);
        if (progress == null)
        {
            return false;
        }

        var projectId = progress.ProjectId;
        var date = progress.Date;

        var deleted = await _projectProgressRepository.DeleteAsync(progressId, userId);
        if (!deleted)
        {
            return false;
        }

        var lastProgress = await _projectProgressRepository.GetLastProgressBeforeAsync(projectId, date);
        var project = await _projectRepository.GetUserProjectByIdAsync(projectId, userId);
        if (project != null)
        {
            project.CurrentWordCount = lastProgress?.TotalWordsWritten ?? 0;
            await _projectRepository.UpdateAsync(project);
        }

        return true;
    }

    public async Task<ProjectStatsDto> GetStatsAsync(Guid projectId, ClaimsPrincipal user)
    {
        var userId = _userService.GetUserId(user);
        var project = await _projectRepository.GetUserProjectByIdAsync(projectId, userId);
        if (project == null)
        {
            return null;
        }

        var entries = await _projectProgressRepository.GetProgressByProjectIdAsync(projectId, userId);
        if (entries == null || !entries.Any())
        {
            return new ProjectStatsDto
            {
                TotalWords = 0,
                AveragePerDay = 0,
                BestDay = null,
                ActiveDays = 0,
                WordsRemaining = ResolveGoalTarget(project),
                MotivationMessage = "Que tal começar hoje mesmo sua primeira escrita?"
            };
        }

        var total = SumByGoalUnit(entries, project.GoalUnit);
        var groupedByDate = entries
            .GroupBy(p => p.Date.Date)
            .Select(g => new ProgressSummary(g.Key, SumByGoalUnit(g, project.GoalUnit)))
            .ToList();

        var averagePerDay = groupedByDate.Any()
            ? (int)groupedByDate.Average(g => g.Total)
            : 0;

        var bestDay = groupedByDate
            .OrderByDescending(g => g.Total)
            .FirstOrDefault();

        var goalTarget = ResolveGoalTarget(project) ?? 0;
        var remaining = goalTarget > 0 ? Math.Max(0, goalTarget - total) : 0;
        var activeDays = groupedByDate.Count;

        var motivationMessage = BuildMotivationMessage(groupedByDate, remaining, averagePerDay);

        return new ProjectStatsDto
        {
            TotalWords = total,
            AveragePerDay = averagePerDay,
            BestDay = bestDay != null
                ? new BestDayDto
                {
                    Date = bestDay.Date.ToString("yyyy-MM-dd"),
                    Words = bestDay.Total
                }
                : null,
            ActiveDays = activeDays,
            WordsRemaining = remaining,
            MotivationMessage = motivationMessage
        };
    }

    public async Task UploadCoverAsync(Guid projectId, ClaimsPrincipal user, byte[] bytes, string mime, int size)
    {
        var userId = _userService.GetUserId(user);
        var project = await _projectRepository.GetUserProjectByIdAsync(projectId, userId)
                       ?? throw new InvalidOperationException("Projeto não encontrado ou sem permissão");

        project.CoverBytes = bytes;
        project.CoverMime = mime;
        project.CoverSize = size;
        project.CoverUpdatedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project);
    }

    public async Task DeleteCoverAsync(Guid projectId, ClaimsPrincipal user)
    {
        var userId = _userService.GetUserId(user);
        var project = await _projectRepository.GetUserProjectByIdAsync(projectId, userId)
                       ?? throw new InvalidOperationException("Projeto não encontrado ou sem permissão");

        project.CoverBytes = null;
        project.CoverMime = null;
        project.CoverSize = null;
        project.CoverUpdatedAt = null;

        await _projectRepository.UpdateAsync(project);
    }

    public async Task<(byte[] bytes, string mime, int size, DateTime? updatedAt)?> GetCoverAsync(Guid projectId, ClaimsPrincipal user)
    {
        var userId = _userService.GetUserId(user);
        var project = await _projectRepository.GetUserProjectByIdAsync(projectId, userId);
        if (project == null || project.CoverBytes == null)
        {
            return null;
        }

        var mime = project.CoverMime ?? "application/octet-stream";
        var size = project.CoverSize ?? project.CoverBytes.Length;
        return (project.CoverBytes, mime, size, project.CoverUpdatedAt);
    }

    public async Task SetFlexibleGoalAsync(
        Guid projectId,
        Guid userId,
        int goalAmount,
        GoalUnit goalUnit,
        DateTime? deadline,
        CancellationToken ct = default)
    {
        if (goalAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(goalAmount), "GoalAmount deve ser >= 0.");
        }

        var ownsProject = await _projectRepository.UserOwnsProjectAsync(projectId, userId, ct);
        if (!ownsProject)
        {
            throw new UnauthorizedAccessException("Você não tem permissão para alterar este projeto.");
        }

        await _projectRepository.UpdateFlexibleGoalAsync(projectId, goalAmount, goalUnit, deadline, ct);
    }

    private static ProjectDto MapToDto(Project project)
    {
        var goalTarget = ResolveGoalTarget(project);
        var progressPercent = goalTarget.HasValue && goalTarget.Value > 0
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
            HasCover = project.CoverBytes != null && project.CoverBytes.Length > 0,
            CoverUpdatedAt = project.CoverUpdatedAt
        };
    }

    private static int? ResolveGoalTarget(Project project)
    {
        if (project.WordCountGoal.HasValue && project.WordCountGoal.Value > 0)
        {
            return project.WordCountGoal.Value;
        }

        return project.GoalAmount > 0 ? project.GoalAmount : null;
    }

    private static int SumByGoalUnit(IEnumerable<ProjectProgress> entries, GoalUnit unit)
    {
        return unit switch
        {
            GoalUnit.Minutes => entries.Sum(p => p.Minutes),
            GoalUnit.Pages => entries.Sum(p => p.Pages),
            _ => entries.Sum(p => p.WordsWritten)
        };
    }

    private static string BuildMotivationMessage(
        IEnumerable<ProgressSummary> groupedEntries,
        int wordsRemaining,
        int averagePerDay)
    {
        if (!groupedEntries.Any())
        {
            return "Que tal começar hoje mesmo sua primeira escrita?";
        }

        var lastEntryDate = groupedEntries.Max(g => g.Date);
        var daysSinceLast = (DateTime.Today - lastEntryDate).Days;

        if (daysSinceLast >= 3)
        {
            return "Vamos lá! Já faz alguns dias desde a última escrita.";
        }

        if (wordsRemaining == 0)
        {
            return "Parabéns! Você alcançou sua meta! 🎉";
        }

        return averagePerDay >= 500
            ? "Ótimo ritmo! Mantenha esse foco!"
            : "Cada palavra conta. Continue escrevendo!";
    }

    private sealed record ProgressSummary(DateTime Date, int Total);
}
