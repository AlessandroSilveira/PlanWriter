using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Queries;

public class GetEventProgressQueryHandler(IProjectEventsRepository projectEventsRepository,
    IProjectProgressRepository projectProgressRepository, ILogger<GetEventProgressQueryHandler> logger)
    : IRequestHandler<GetEventProgressQuery, EventProgressDto?>
{
    public async Task<EventProgressDto?> Handle(GetEventProgressQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting event progress for project {ProjectId} and event {EventId}", request.ProjectId, request.EventId);

        var projectEvent = await projectEventsRepository
            .GetProjectEventByProjectIdAndEventId(request.ProjectId, request.EventId) 
                           ?? throw new KeyNotFoundException("Inscrição do projeto no evento não encontrada.");

        var ev = projectEvent.Event!;
        var target = ResolveTargetWords(projectEvent, ev);
        var totalInEvent = await GetTotalWordsInEventAsync(request.ProjectId, ev);
        var progress = CalculateProgress(target, totalInEvent, ev.StartsAtUtc, ev.EndsAtUtc);

        return new EventProgressDto(
            request.ProjectId,
            request.EventId,
            target,
            totalInEvent,
            progress.Percent,
            progress.Remaining,
            progress.Days,
            progress.DayIndex,
            progress.DailyTarget,
            projectEvent.Id,
            projectEvent.ValidatedAtUtc,
            projectEvent.Won,
            ev.Name
        );
    }

    private static int ResolveTargetWords(Domain.Events.ProjectEvent projectEvent, Domain.Events.Event ev) 
        => projectEvent.TargetWords ?? ev.DefaultTargetWords ?? 50000;

    private async Task<int> GetTotalWordsInEventAsync(Guid projectId, Domain.Events.Event ev)
    {
        var entries = await projectProgressRepository.FindAsync(w =>
            w.ProjectId == projectId &&
            w.CreatedAt >= ev.StartsAtUtc &&
            w.CreatedAt < ev.EndsAtUtc
        );

        return entries.Sum(w => w.WordsWritten);
    }

    private static EventProgressCalculation CalculateProgress(int target, int total, DateTime start, DateTime end)
    {
        var days = Math.Max(1, (int)Math.Ceiling((end - start).TotalDays));

        var dayIndex = Math.Clamp((int)Math.Ceiling((DateTime.UtcNow - start).TotalDays), 1, days);

        var dailyTarget = (int)Math.Ceiling((double)target / days);
        var percent = target > 0
            ? (int)Math.Round(total * 100.0 / target)
            : 0;

        var remaining = Math.Max(0, target - total);

        return new EventProgressCalculation(
            days,
            dayIndex,
            dailyTarget,
            percent,
            remaining
        );
    }

    private record EventProgressCalculation(
        int Days,
        int DayIndex,
        int DailyTarget,
        int Percent,
        int Remaining
    );
}
