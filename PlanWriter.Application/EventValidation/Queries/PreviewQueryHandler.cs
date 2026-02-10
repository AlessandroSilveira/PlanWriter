using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.EventValidation.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;

namespace PlanWriter.Application.EventValidation.Queries;

public class PreviewQueryHandler(
    ILogger<PreviewQueryHandler> logger, 
    IProjectProgressReadRepository projectProgressReadRepository, 
    IProjectEventsReadRepository projectEventsReadRepository,
    IEventReadRepository eventReadRepository,
    IProjectReadRepository projectReadRepository
    )
    : IRequestHandler<PreviewQuery, (int TargetWords, int TotalWords)>
{
    public async Task<(int TargetWords, int TotalWords)> Handle(PreviewQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Previewing event progress for Project {ProjectId} and Event {EventId}", request.ProjectId, request.EventId);

        var (eventEntity, projectEvent) = await LoadContextAsync(request.CurrentUserId, request.EventId, request.ProjectId, cancellationToken);
        
        var targetWords = ResolveTargetWords(eventEntity, projectEvent);

        logger.LogInformation("Target words resolved as {TargetWords}", targetWords);

        var totalWordsWritten = await CalculateTotalWordsAsync(request.ProjectId, request.CurrentUserId, eventEntity, cancellationToken);

        logger.LogInformation("Total words written so far in event window: {TotalWords}", totalWordsWritten);

        return (targetWords, totalWordsWritten);
    }

    private async Task<(EventDto Event, ProjectEvent ProjectEvent)> LoadContextAsync(Guid userId, Guid eventId,
        Guid projectId, CancellationToken cancellationToken)
    {
        var eventEntity = await eventReadRepository.GetEventByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento não encontrado.");

        var project = await projectReadRepository.GetProjectByIdAsync(projectId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Projeto não encontrado ou não pertence ao usuário.");

        var projectEvent = await projectEventsReadRepository.GetByProjectAndEventWithEventAsync(projectId, eventId, cancellationToken)
                           ?? throw new InvalidOperationException("Projeto não está inscrito neste evento.");

        return (eventEntity, projectEvent);
    }

    private static int ResolveTargetWords(EventDto eventEntity, ProjectEvent projectEvent) =>
        projectEvent.TargetWords ?? eventEntity.DefaultTargetWords ?? 50000;

    private async Task<int> CalculateTotalWordsAsync(Guid projectId, Guid userId, EventDto eventEntity, CancellationToken cancellationToken)
    {
        var progressEntries = await projectProgressReadRepository.GetProgressByProjectIdAsync(projectId, userId, cancellationToken);

        var total =
            progressEntries
                .Where(w =>
                    w.ProjectId == projectId &&
                    w.CreatedAt >= eventEntity.StartsAtUtc &&
                    w.CreatedAt < eventEntity.EndsAtUtc)
                .Sum(w => (int?)w.WordsWritten) ?? 0;

        return total;
    }
}
