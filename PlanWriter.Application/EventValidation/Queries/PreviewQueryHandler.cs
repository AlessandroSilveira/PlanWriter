using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.EventValidation.Dtos.Queries;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.EventValidation.Queries;

public class PreviewQueryHandler(IProjectProgressRepository projectProgressRepository, IEventRepository eventRepository,
    IProjectRepository projectRepository, IProjectEventsRepository projectEventsRepository, ILogger<PreviewQueryHandler> logger, IProjectProgressReadRepository projectProgressReadRepository)
    : IRequestHandler<PreviewQuery, (int TargetWords, int TotalWords)>
{
    public async Task<(int TargetWords, int TotalWords)> Handle(PreviewQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Previewing event progress for Project {ProjectId} and Event {EventId}", request.ProjectId, request.EventId);

        var (eventEntity, projectEvent) = await LoadContextAsync(request.CurrentUserId, request.EventId, request.ProjectId);
        var targetWords = ResolveTargetWords(eventEntity, projectEvent);

        logger.LogInformation("Target words resolved as {TargetWords}", targetWords);

        var totalWordsWritten = await CalculateTotalWordsAsync(request.ProjectId, request.CurrentUserId, eventEntity);

        logger.LogInformation("Total words written so far in event window: {TotalWords}", totalWordsWritten);

        return (targetWords, totalWordsWritten);
    }

    private async Task<(Event Event, ProjectEvent ProjectEvent)> LoadContextAsync(Guid userId, Guid eventId, Guid projectId)
    {
        var eventEntity = await eventRepository.GetEventById(eventId)
            ?? throw new KeyNotFoundException("Evento não encontrado.");

        var project = await projectRepository.GetProjectById(projectId)
            ?? throw new InvalidOperationException("Projeto não encontrado ou não pertence ao usuário.");

        var projectEvent = await projectEventsRepository.GetProjectEventByProjectIdAndEventId(projectId, eventId)
                           ?? throw new InvalidOperationException("Projeto não está inscrito neste evento.");

        return (eventEntity, projectEvent);
    }

    private static int ResolveTargetWords(Event eventEntity, ProjectEvent projectEvent) =>
        projectEvent.TargetWords ?? eventEntity.DefaultTargetWords ?? 50000;

    private async Task<int> CalculateTotalWordsAsync(Guid projectId, Guid userId, Event eventEntity)
    {
        var progressEntries = await projectProgressReadRepository.GetProgressByProjectIdAsync(projectId, userId);

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
