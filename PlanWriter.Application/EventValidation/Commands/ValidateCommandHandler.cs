using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.EventValidation;
using PlanWriter.Application.EventValidation.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.EventValidation.Commands;

public class ValidateCommandHandler(
    ILogger<ValidateCommandHandler> logger, 
    IProjectRepository projectRepository, 
    IProjectEventsRepository projectEventsRepository, 
    IProjectEventsReadRepository projectEventsReadRepository,
    IEventReadRepository eventReadRepository,
    IEventValidationAuditRepository eventValidationAuditRepository) : IRequestHandler<ValidateCommand, Unit>
{
    public async Task<Unit> Handle(ValidateCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Validating project {ProjectId} for event {EventId}", request.ProjectId, request.EventId);

        var now = DateTime.UtcNow;
        var normalizedSource = ValidationPolicyHelper.NormalizeSource(request.Source);
        var (eventEntity, projectEvent) = await LoadContextAsync(request.CurrentUserId, request.EventId, request.ProjectId, cancellationToken);

        var targetWords = projectEvent.TargetWords ?? eventEntity.DefaultTargetWords ?? 50000;
        var (validationWindowStartsAtUtc, validationWindowEndsAtUtc) = ValidationPolicyHelper.ResolveValidationWindow(
            eventEntity.StartsAtUtc,
            eventEntity.EndsAtUtc,
            eventEntity.ValidationWindowStartsAtUtc,
            eventEntity.ValidationWindowEndsAtUtc);

        var allowedSources = ValidationPolicyHelper.ParseAllowedSources(eventEntity.AllowedValidationSources);

        if (!allowedSources.Contains(normalizedSource))
        {
            const string sourceRuleMessage = "Fonte de validação não permitida para este evento.";
            await PersistAuditAsync(
                request,
                normalizedSource,
                "rejected",
                null,
                sourceRuleMessage,
                cancellationToken);

            throw new BusinessRuleException(sourceRuleMessage);
        }

        if (now < validationWindowStartsAtUtc || now > validationWindowEndsAtUtc)
        {
            var windowRuleMessage = $"Validação fora da janela permitida. Janela: {validationWindowStartsAtUtc:O} até {validationWindowEndsAtUtc:O}.";
            await PersistAuditAsync(
                request,
                normalizedSource,
                "rejected",
                null,
                windowRuleMessage,
                cancellationToken);

            throw new BusinessRuleException(windowRuleMessage);
        }

        if (request.Words < targetWords)
        {
            var wordsRuleMessage = $"Total informado ({request.Words}) é menor que a meta ({targetWords}).";
            await PersistAuditAsync(
                request,
                normalizedSource,
                "rejected",
                null,
                wordsRuleMessage,
                cancellationToken);

            throw new BusinessRuleException(wordsRuleMessage);
        }

        projectEvent.Won = true;
        projectEvent.ValidatedAtUtc = now;
        projectEvent.ValidatedWords = request.Words;
        projectEvent.FinalWordCount = request.Words;
        projectEvent.ValidationSource = normalizedSource;

        await projectEventsRepository.UpdateProjectEvent(projectEvent, cancellationToken);
        await PersistAuditAsync(request, normalizedSource, "approved", now, null, cancellationToken);

        logger.LogInformation("ProjectEvent {ProjectEventId} validated successfully", projectEvent.Id);

        return Unit.Value;
    }

    private async Task<(EventDto Event, ProjectEvent ProjectEvent)> LoadContextAsync(Guid userId, Guid eventId, Guid projectId, CancellationToken cancellationToken)
    {
        var eventEntity = await eventReadRepository.GetEventByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento não encontrado.");

        var project = await projectRepository.GetProjectById(projectId)
            ?? throw new InvalidOperationException("Projeto não encontrado ou não pertence ao usuário.");

        var projectEvent = await projectEventsReadRepository.GetByProjectAndEventWithEventAsync(projectId, eventId, cancellationToken)
                           ?? throw new InvalidOperationException("Projeto não está inscrito neste evento.");

        return (eventEntity, projectEvent);
    }

    private Task PersistAuditAsync(
        ValidateCommand request,
        string source,
        string status,
        DateTime? validatedAtUtc,
        string? reason,
        CancellationToken ct)
        => eventValidationAuditRepository.CreateAsync(
            request.EventId,
            request.ProjectId,
            request.CurrentUserId,
            source,
            request.Words,
            status,
            validatedAtUtc,
            reason,
            ct);
}
