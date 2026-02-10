using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Commands;

public class FinalizeEventCommandHandler(
    IProjectEventsRepository projectEventsRepository,
    IEventRepository eventRepository,
    IProjectProgressRepository projectProgressRepository,
    IBadgeRepository badgeRepository,
    ILogger<FinalizeEventCommandHandler> logger,IProjectEventsReadRepository projectEventsReadRepository)
    : IRequestHandler<FinalizeEventCommand, ProjectEvent>
{
    public async Task<ProjectEvent> Handle(FinalizeEventCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Finalizing event participation for ProjectEvent {ProjectEventId}", request.Req.ProjectEventId);

        // 1️⃣ Recupera a inscrição do projeto no evento
        var projectEvent = await projectEventsReadRepository.GetByIdWithEventAsync(request.Req.ProjectEventId, cancellationToken)
            ?? throw new KeyNotFoundException("Inscrição não encontrada.");

        logger.LogInformation("ProjectEvent {ProjectEventId} found for Project {ProjectId}", projectEvent.Id, projectEvent.ProjectId);

        // 2️⃣ Recupera o evento (lazy load / fallback)
        var eventEntity = projectEvent.Event
            ?? await eventRepository.GetEventById(projectEvent.EventId)
            ?? throw new KeyNotFoundException("Evento não encontrado.");

        logger.LogInformation("Event {EventId} ({EventName}) loaded for finalization", eventEntity.Id, eventEntity.Name);

        // 3️⃣ Resolve a meta de palavras
        var targetWordCount = projectEvent.TargetWords
                              ?? eventEntity.DefaultTargetWords
                              ?? 50000;

        logger.LogInformation("Target word count resolved as {TargetWords} words", targetWordCount);

        // 4️⃣ Soma das palavras escritas durante o período do evento
        var progressEntries = await projectProgressRepository.GetByProjectAndDateRangeAsync(
            projectEvent.ProjectId!.Value,
            eventEntity.StartsAtUtc,
            eventEntity.EndsAtUtc,
            cancellationToken);

        var totalWordsWrittenInEvent = progressEntries.Sum(w => (int?)w.WordsWritten) ?? 0;

        logger.LogInformation("Total words written during event: {TotalWords}", totalWordsWrittenInEvent);

        // 5️⃣ Finaliza a inscrição
        projectEvent.FinalWordCount = totalWordsWrittenInEvent;
        projectEvent.ValidatedWords = totalWordsWrittenInEvent;
        projectEvent.ValidatedAtUtc = DateTime.UtcNow;
        projectEvent.Won = totalWordsWrittenInEvent >= targetWordCount;

        logger.LogInformation("ProjectEvent {ProjectEventId} finalized. Won = {Won}", projectEvent.Id, projectEvent.Won);

        await projectEventsRepository.UpdateProjectEvent(projectEvent, cancellationToken);

        // 6️⃣ Criação do badge (winner ou participant)
        var badge = CreateEventBadge(
            projectEvent,
            eventEntity,
            totalWordsWrittenInEvent,
            targetWordCount
        );

        await badgeRepository.SaveAsync(new List<Badge> { badge });

        logger.LogInformation("Badge '{BadgeName}' awarded to Project {ProjectId}", badge.Name, badge.ProjectId);

        return projectEvent;
    }

    /* ===================== PRIVATE METHODS ===================== */

    private static Badge CreateEventBadge(ProjectEvent projectEvent, Event eventEntity, int totalWordsWritten, int targetWords)
    {
        var isWinner = projectEvent.Won;

        return new Badge
        {
            ProjectId = projectEvent.ProjectId!.Value,
            EventId = eventEntity.Id,
            Name = isWinner
                ? $"🏆 Winner — {eventEntity.Name}"
                : $"🎉 Participant — {eventEntity.Name}",
            Description = isWinner
                ? $"Você atingiu a meta de {targetWords:N0} palavras no {eventEntity.Name}!"
                : $"Obrigado por participar do {eventEntity.Name}. Continue escrevendo!",
            Icon = isWinner ? "🏆" : "🎉",
            AwardedAt = DateTime.UtcNow
        };
    }
}
