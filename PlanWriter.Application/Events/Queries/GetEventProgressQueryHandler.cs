using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Events;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Queries;

public class GetEventProgressQueryHandler(
    IProjectProgressRepository projectProgressRepository, 
    ILogger<GetEventProgressQueryHandler> logger, 
    IProjectEventsReadRepository projectEventsReadRepository,
    IEventProgressCalculator eventProgressCalculator)
    : IRequestHandler<GetEventProgressQuery, EventProgressDto?>
{
    public async Task<EventProgressDto?> Handle(GetEventProgressQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting event progress for project {ProjectId} and event {EventId}", request.ProjectId, request.EventId);

        var projectEvent = await projectEventsReadRepository
            .GetByProjectAndEventWithEventAsync(request.ProjectId, request.EventId, cancellationToken) 
                           ?? throw new KeyNotFoundException("Inscrição do projeto no evento não encontrada.");

        var ev = projectEvent.Event!;
        var totalInEvent = await GetTotalWordsInEventAsync(request.ProjectId, ev, cancellationToken);
        var metrics = eventProgressCalculator.Calculate(
            projectEvent.TargetWords,
            ev.DefaultTargetWords,
            totalInEvent);
        var progress = CalculateTimelineProgress(metrics.TargetWords, ev.StartsAtUtc, ev.EndsAtUtc);

        return new EventProgressDto(
            request.ProjectId,
            request.EventId,
            metrics.TargetWords,
            metrics.TotalWords,
            metrics.Percent,
            metrics.RemainingWords,
            progress.Days,
            progress.DayIndex,
            progress.DailyTarget,
            projectEvent.Id,
            projectEvent.ValidatedAtUtc,
            metrics.Won,
            ev.Name
        );
    }

    private async Task<int> GetTotalWordsInEventAsync(Guid projectId, Event ev, CancellationToken cancellationToken)
    {
        var endExclusive = eventProgressCalculator.ResolveWindowEndExclusive(ev.EndsAtUtc);
        var entries = await projectProgressRepository.GetByProjectAndDateRangeAsync(
            projectId,
            ev.StartsAtUtc,
            endExclusive,
            cancellationToken);

        return entries.Sum(w => w.WordsWritten);
    }

    private static EventTimelineProgress CalculateTimelineProgress(int target, DateTime start, DateTime end)
    {
        var startDate = start.Date;
        var endDate = end.Date;
        var days = Math.Max(1, (endDate - startDate).Days + 1);

        var dayIndex = Math.Clamp((DateTime.UtcNow.Date - startDate).Days + 1, 1, days);

        var dailyTarget = (int)Math.Ceiling((double)target / days);
        return new EventTimelineProgress(
            days,
            dayIndex,
            dailyTarget
        );
    }

    private record EventTimelineProgress(
        int Days,
        int DayIndex,
        int DailyTarget
    );
}
