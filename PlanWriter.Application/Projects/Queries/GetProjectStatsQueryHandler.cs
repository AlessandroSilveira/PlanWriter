using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;

namespace PlanWriter.Application.Projects.Queries;

public class GetProjectStatsQueryHandler(ILogger<GetProjectStatsQueryHandler> logger, IProjectReadRepository projectRepository,
    IProjectProgressReadRepository projectProgressRepository) : IRequestHandler<GetProjectStatsQuery, ProjectStatsDto?>
{
    public async Task<ProjectStatsDto?> Handle(GetProjectStatsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting stats for project {ProjectId} and user {UserId}", request.ProjectId, request.UserId);
        var project =
            await projectRepository.GetProjectByIdAsync(request.ProjectId, request.UserId,cancellationToken);

        if (project is null)
            return null;

        var goalTarget = ResolveGoalTarget(project);
        var today = DateTime.UtcNow.Date;
        var startDate = project.StartDate.Date;
        var endDate = project.Deadline?.Date;
        var daysTotal = ResolveTotalDays(startDate, endDate);

        var dailyProgress = (await projectProgressRepository.GetProgressByDayAsync(request.ProjectId, request.UserId, cancellationToken))
            .ToList();

        if (!dailyProgress.Any())
            return BuildEmptyStats(goalTarget, startDate, endDate, today, daysTotal);

        logger.LogInformation("Found {Count} progress entries for project {ProjectId}", dailyProgress.Count, request.ProjectId);
        return BuildStatsWithProgress(
            project,
            dailyProgress,
            goalTarget,
            startDate,
            endDate,
            today,
            daysTotal);
    }


    private static ProjectStatsDto BuildEmptyStats(int goalTarget, DateTime startDate, DateTime? endDate,
        DateTime today, int daysTotal)
    {
        return new ProjectStatsDto
        {
            TotalWords = 0,
            AveragePerDay = 0,
            BestDay = null,
            ActiveDays = 0,
            WordsRemaining = goalTarget > 0 ? goalTarget : 0,
            MotivationMessage = "Que tal começar hoje mesmo sua primeira escrita?",
            TargetPerDay = CalcTargetPerDay(goalTarget, daysTotal),
            Status = ResolveStatus(0, goalTarget, startDate, today, endDate),
            StatusReason = "Sem lançamentos ainda."
        };
    }

    private static ProjectStatsDto BuildStatsWithProgress(ProjectDto project, IEnumerable<ProjectProgressDayDto> entries, int goalTarget, DateTime startDate,
        DateTime? endDate, DateTime today, int daysTotal)
    {
        var total = SumByGoalUnit(entries, project.GoalUnit);

        var groupedByDate = entries
            .GroupBy(p => p.Date.Date)
            .Select(g => new ProgressSummary(
                g.Key,
                SumByGoalUnit(g, project.GoalUnit)
            ))
            .OrderBy(g => g.Date)
            .ToList();

        var activeDays = groupedByDate.Count;

        var averagePerDay = activeDays > 0 ? (int)Math.Round(groupedByDate.Average(g => g.Total)) : 0;

        var bestDay = groupedByDate.OrderByDescending(g => g.Total).FirstOrDefault();

        var remaining = goalTarget > 0 ? Math.Max(0, goalTarget - total) : 0;

        var daysElapsed = Math.Min(Math.Max(1, (today - startDate).Days + 1), daysTotal);

        var targetPerDay = CalcTargetPerDay(goalTarget, daysTotal);

        var expectedAcc = goalTarget > 0 ? (int)Math.Round((double)goalTarget * daysElapsed / daysTotal) : 0;

        var delta = total - expectedAcc;

        var status = ResolveStatus(
            total,
            goalTarget,
            startDate,
            today,
            endDate
        );

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
            MotivationMessage =
                BuildMotivationMessage(
                    groupedByDate,
                    remaining,
                    averagePerDay
                ),
            TargetPerDay = targetPerDay,
            Status = status,
            StatusReason = BuildStatusReason(status, delta)
        };
    }

    /* ===================== HELPERS ===================== */

    private static int ResolveTotalDays(DateTime startDate, DateTime? endDate) 
        => endDate.HasValue ? Math.Max(1, (endDate.Value - startDate).Days + 1) : 30;

    private static int ResolveGoalTarget(ProjectDto project)
    {
        if (project.WordCountGoal.HasValue && project.WordCountGoal.Value > 0)
            return project.WordCountGoal.Value;

        return project.WordCountGoal > 0 ? project.WordCountGoal.Value : 0;
    }

    private static int CalcTargetPerDay(int goal, int daysTotal)
        => goal > 0 && daysTotal > 0 ? (int)Math.Ceiling((double)goal / daysTotal) : 0;

    private static ProjectStatus ResolveStatus(int total, int goal, DateTime startDate, DateTime today, DateTime? deadline)
    {
        if (goal > 0 && total >= goal)
            return ProjectStatus.Completed;

        if (goal <= 0)
            return total > 0 ? ProjectStatus.OnTrack : ProjectStatus.NotStarted;

        var endDate = deadline ?? startDate.AddDays(29);
        var daysTotal = Math.Max(1, (endDate.Date - startDate.Date).Days + 1);

        var daysElapsed = Math.Min(Math.Max(1, (today - startDate).Days + 1), daysTotal);

        var expectedAcc = (int)Math.Round((double)goal * daysElapsed / daysTotal);

        var delta = total - expectedAcc;

        if (delta >= 0) return ProjectStatus.OnTrack;

        var pctBehind = expectedAcc > 0 ? (double)delta / expectedAcc : -1;

        return pctBehind > -0.15 ? ProjectStatus.AtRisk : ProjectStatus.Behind;
    }

    private static string? BuildStatusReason(ProjectStatus status, int deltaVsPace)
    {
        return status switch
        {
            ProjectStatus.Completed => "Meta alcançada.",
            ProjectStatus.NotStarted => "Sem meta definida ou início recente.",
            ProjectStatus.OnTrack => "Acima ou no pace.",
            ProjectStatus.AtRisk => $"Pouco abaixo do pace ({deltaVsPace}).",
            ProjectStatus.Behind => $"Abaixo do pace ({deltaVsPace}).",
            _ => null
        };
    }

    private static string BuildMotivationMessage(IEnumerable<ProgressSummary> groupedEntries, int wordsRemaining, int averagePerDay)
    {
        if (!groupedEntries.Any())
            return "Que tal começar hoje mesmo sua primeira escrita?";

        var lastEntryDate = groupedEntries.Max(g => g.Date);
        var daysSinceLast = (DateTime.Today - lastEntryDate).Days;

        if (daysSinceLast >= 3)
            return "Vamos lá! Já faz alguns dias desde a última escrita.";

        if (wordsRemaining == 0)
            return "Parabéns! Você alcançou sua meta! 🎉";

        return averagePerDay >= 500 ? "Ótimo ritmo! Mantenha esse foco!" : "Cada palavra conta. Continue escrevendo!";
    }

    private static int SumByGoalUnit(IEnumerable<ProjectProgressDayDto> entries, GoalUnit unit)
    {
        return unit switch
        {
            GoalUnit.Minutes => entries.Sum(p => p.TotalMinutes),
            GoalUnit.Pages => entries.Sum(p => p.TotalPages),
            _ => entries.Sum(p => p.TotalWords)
        };
    }
}