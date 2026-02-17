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

public sealed class ValidationStatusQueryHandler(
    ILogger<ValidationStatusQueryHandler> logger,
    IProjectProgressReadRepository projectProgressReadRepository,
    IProjectEventsReadRepository projectEventsReadRepository,
    IEventReadRepository eventReadRepository,
    IProjectReadRepository projectReadRepository)
    : IRequestHandler<ValidationStatusQuery, ValidationStatusDto>
{
    public async Task<ValidationStatusDto> Handle(ValidationStatusQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Getting validation status for Project {ProjectId} and Event {EventId}",
            request.ProjectId,
            request.EventId);

        var (eventEntity, projectEvent) = await LoadContextAsync(
            request.CurrentUserId,
            request.EventId,
            request.ProjectId,
            cancellationToken);

        var targetWords = projectEvent.TargetWords ?? eventEntity.DefaultTargetWords ?? 50000;
        var totalWords = await CalculateTotalWordsAsync(
            request.ProjectId,
            request.CurrentUserId,
            eventEntity,
            cancellationToken);

        var (validationWindowStartsAtUtc, validationWindowEndsAtUtc) = ValidationPolicyHelper.ResolveValidationWindow(
            eventEntity.StartsAtUtc,
            eventEntity.EndsAtUtc,
            eventEntity.ValidationWindowStartsAtUtc,
            eventEntity.ValidationWindowEndsAtUtc);

        var allowedSources = ValidationPolicyHelper.ParseAllowedSources(eventEntity.AllowedValidationSources);
        var now = DateTime.UtcNow;
        var isWithinValidationWindow = now >= validationWindowStartsAtUtc && now <= validationWindowEndsAtUtc;
        var isValidated = projectEvent.Won && projectEvent.ValidatedAtUtc.HasValue;

        var blockReason = ResolveBlockReason(
            isValidated,
            isWithinValidationWindow,
            totalWords,
            targetWords);

        var canValidate = blockReason is null;

        return new ValidationStatusDto(
            targetWords,
            totalWords,
            isValidated,
            projectEvent.ValidatedAtUtc,
            projectEvent.ValidatedWords,
            projectEvent.ValidationSource,
            validationWindowStartsAtUtc,
            validationWindowEndsAtUtc,
            isWithinValidationWindow,
            canValidate,
            blockReason,
            allowedSources);
    }

    private async Task<(EventDto Event, ProjectEvent ProjectEvent)> LoadContextAsync(
        Guid userId,
        Guid eventId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventReadRepository.GetEventByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento não encontrado.");

        var project = await projectReadRepository.GetProjectByIdAsync(projectId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Projeto não encontrado ou não pertence ao usuário.");

        var projectEvent = await projectEventsReadRepository.GetByProjectAndEventWithEventAsync(projectId, eventId, cancellationToken)
            ?? throw new InvalidOperationException("Projeto não está inscrito neste evento.");

        return (eventEntity, projectEvent);
    }

    private async Task<int> CalculateTotalWordsAsync(
        Guid projectId,
        Guid userId,
        EventDto eventEntity,
        CancellationToken cancellationToken)
    {
        var progressEntries = await projectProgressReadRepository.GetProgressByProjectIdAsync(projectId, userId, cancellationToken);

        return progressEntries
            .Where(p =>
                p.ProjectId == projectId &&
                p.CreatedAt >= eventEntity.StartsAtUtc &&
                p.CreatedAt < eventEntity.EndsAtUtc)
            .Sum(p => (int?)p.WordsWritten) ?? 0;
    }

    private static string? ResolveBlockReason(
        bool isValidated,
        bool isWithinValidationWindow,
        int totalWords,
        int targetWords)
    {
        if (isValidated)
            return "Projeto já validado neste evento.";

        if (!isWithinValidationWindow)
            return "Validação fora da janela permitida.";

        if (totalWords < targetWords)
            return $"Faltam {targetWords - totalWords} palavras para atingir a meta.";

        return null;
    }
}
