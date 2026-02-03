using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Projects.Commands;

public class AddProjectProgressCommandHandler(IProjectRepository projectRepository, IProjectProgressRepository projectProgressRepository,
    IMediator mediator, ILogger<AddProjectProgressCommandHandler> logger, IProjectReadRepository projectReadRepository)
    : IRequestHandler<AddProjectProgressCommand, bool>
{
    public async Task<bool> Handle(AddProjectProgressCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding project progress. ProjectId={ProjectId} UserId={UserId}", request.Request.ProjectId, request.UserId);

        ValidateRequest(request);

        var projectId = request.Request.ProjectId;
        var userId = request.UserId;

        var project = await projectReadRepository.GetUserProjectByIdAsync(projectId, userId, cancellationToken) ?? throw new KeyNotFoundException("Project not found");

        logger.LogInformation(
            "Project loaded. ProjectId={ProjectId} GoalUnit={GoalUnit} CurrentWordCount={CurrentWordCount}",
            project.Id, project.GoalUnit, project.CurrentWordCount);

        var incrementValue = ResolveIncrementValue(project.GoalUnit, request);

        if (incrementValue <= 0)
        {
            // (extra defesa — não deveria ocorrer por causa da ValidateRequest)
            logger.LogWarning("Increment resolved to <= 0. ProjectId={ProjectId} UserId={UserId}",
                projectId, userId);
            throw new InvalidOperationException("Increment must be > 0.");
        }

        var previousTotal = project.CurrentWordCount;
        var newTotal = previousTotal + incrementValue;

        logger.LogInformation("Progress increment resolved. ProjectId={ProjectId} PreviousTotal={PreviousTotal} Increment={Increment} NewTotal={NewTotal}",
            projectId, previousTotal, incrementValue, newTotal);

        project.CurrentWordCount = newTotal;

        var progressEntity = CreateProgressEntity(project, request, newTotal);

        await projectProgressRepository.AddProgressAsync(progressEntity, cancellationToken);
        await projectRepository.UpdateAsync(project);

        logger.LogInformation("Progress persisted. ProgressId={ProgressId} ProjectId={ProjectId}",
            progressEntity.Id, projectId);

        // Side-effects via notifications (milestones/badges/etc ficam fora)
        await mediator.Publish(
            new ProjectProgressAdded(
                project.Id,
                userId,
                newTotal,
                project.GoalUnit
            ),
            cancellationToken
        );

        logger.LogInformation("ProjectProgressAdded published. ProjectId={ProjectId} UserId={UserId} NewTotal={NewTotal}",
            projectId, userId, newTotal);

        return true;
    }

    /* ===================== HELPERS ===================== */

    private static void ValidateRequest(AddProjectProgressCommand request)
    {
        if (request.Request is null)
            throw new ArgumentNullException(nameof(request.Request));

        var wordsWritten = request.Request.WordsWritten.GetValueOrDefault();
        var minutesWritten = request.Request.Minutes.GetValueOrDefault();
        var pagesWritten = request.Request.Pages.GetValueOrDefault();

        if (wordsWritten <= 0 && minutesWritten <= 0 && pagesWritten <= 0)
            throw new InvalidOperationException("Informe WordsWritten, Minutes ou Pages com valor > 0.");
    }

    private static int ResolveIncrementValue(GoalUnit goalUnit, AddProjectProgressCommand request)
    {
        var words = Math.Max(0, request.Request.WordsWritten ?? 0);
        var minutes = Math.Max(0, request.Request.Minutes ?? 0);
        var pages = Math.Max(0, request.Request.Pages ?? 0);

        var increment = goalUnit switch
        {
            GoalUnit.Minutes => minutes,
            GoalUnit.Pages => pages,
            _ => words
        };

        // fallback: se a unidade escolhida veio 0, pega o maior dentre os três
        return increment > 0 ? increment : new[] { words, minutes, pages }.Max();
    }

    private static ProjectProgress CreateProgressEntity(Project project, AddProjectProgressCommand request, int newTotal)
    {
        var goalTarget = ResolveGoalTarget(project);

        var remainingWords = goalTarget.HasValue
            ? Math.Max(0, goalTarget.Value - newTotal)
            : 0;

        var remainingPercent = goalTarget.HasValue && goalTarget.Value > 0
            ? Math.Round((double)newTotal / goalTarget.Value * 100, 2)
            : 0;

        var effectiveDate = request.Request.Date == default
            ? DateTime.UtcNow
            : request.Request.Date;

        return new ProjectProgress
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            WordsWritten = request.Request.WordsWritten ?? 0,
            Minutes = request.Request.Minutes ?? 0,
            Pages = request.Request.Pages ?? 0,
            TotalWordsWritten = newTotal,
            RemainingWords = remainingWords,
            RemainingPercentage = remainingPercent,
            Date = effectiveDate,
            Notes = request.Request.Notes,
            TimeSpentInMinutes = request.Request.Minutes ?? 0
        };
    }

    private static int? ResolveGoalTarget(Project project)
    {
        if (project.WordCountGoal.HasValue && project.WordCountGoal.Value > 0)
            return project.WordCountGoal.Value;

        return project.GoalAmount > 0 ? project.GoalAmount : null;
    }
}
