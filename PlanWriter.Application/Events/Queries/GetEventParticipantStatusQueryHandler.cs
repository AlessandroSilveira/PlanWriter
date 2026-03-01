using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Events;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Common.WinnerEligibility;
using PlanWriter.Application.EventValidation;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;

namespace PlanWriter.Application.Events.Queries;

public sealed class GetEventParticipantStatusQueryHandler(
    ILogger<GetEventParticipantStatusQueryHandler> logger,
    IEventReadRepository eventReadRepository,
    IProjectReadRepository projectReadRepository,
    IProjectEventsReadRepository projectEventsReadRepository,
    IProjectProgressReadRepository projectProgressReadRepository,
    IEventProgressCalculator eventProgressCalculator,
    IWinnerEligibilityService winnerEligibilityService,
    IEventLifecycleService eventLifecycleService)
    : IRequestHandler<GetEventParticipantStatusQuery, EventParticipantStatusDto>
{
    private const string EventStatusScheduled = "scheduled";
    private const string EventStatusActive = "active";
    private const string EventStatusClosed = "closed";
    private const string EventStatusDisabled = "disabled";

    public async Task<EventParticipantStatusDto> Handle(GetEventParticipantStatusQuery request, CancellationToken cancellationToken)
    {
        await eventLifecycleService.SyncEventIfExpiredAsync(request.EventId, cancellationToken);

        var nowUtc = DateTime.UtcNow;

        var eventEntity = await eventReadRepository.GetEventByIdAsync(request.EventId, cancellationToken)
                          ?? throw new NotFoundException("Evento não encontrado.");

        var project = await projectReadRepository.GetUserProjectByIdAsync(request.ProjectId, request.UserId, cancellationToken)
                      ?? throw new NotFoundException("Projeto não encontrado.");

        var projectEvent = await projectEventsReadRepository.GetByProjectAndEventWithEventAsync(
                               request.ProjectId,
                               request.EventId,
                               cancellationToken)
                           ?? throw new NotFoundException("Projeto não está inscrito neste evento.");

        var effectiveEventStatus = ResolveEffectiveEventStatus(
            nowUtc,
            eventEntity.IsActive,
            eventEntity.StartsAtUtc,
            eventEntity.EndsAtUtc);

        var totalWordsInWindow = await GetTotalWordsInEventWindowAsync(
            request.ProjectId,
            request.UserId,
            eventEntity,
            cancellationToken);

        var effectiveTotalWords = ResolveEffectiveTotalWords(totalWordsInWindow, projectEvent, effectiveEventStatus);
        var progressMetrics = eventProgressCalculator.Calculate(
            projectEvent.TargetWords,
            eventEntity.DefaultTargetWords,
            effectiveTotalWords);

        var (validationWindowStartsAtUtc, validationWindowEndsAtUtc) = ValidationPolicyHelper.ResolveValidationWindow(
            eventEntity.StartsAtUtc,
            eventEntity.EndsAtUtc,
            eventEntity.ValidationWindowStartsAtUtc,
            eventEntity.ValidationWindowEndsAtUtc);

        var isValidationWindowOpen = nowUtc >= validationWindowStartsAtUtc && nowUtc <= validationWindowEndsAtUtc;
        var isValidated = projectEvent.ValidatedAtUtc.HasValue;

        var eligibility = winnerEligibilityService.EvaluateForGoodies(
            nowUtc,
            eventEntity.EndsAtUtc,
            progressMetrics.TargetWords,
            progressMetrics.TotalWords,
            isValidated,
            projectEvent.Won);

        var validationBlockReason = ResolveValidationBlockReason(
            isValidated,
            isValidationWindowOpen,
            progressMetrics.TotalWords,
            progressMetrics.TargetWords);

        var canValidate = validationBlockReason is null && eligibility.CanValidate;
        var allowedSources = ValidationPolicyHelper.ParseAllowedSources(eventEntity.AllowedValidationSources);

        logger.LogInformation(
            "Unified participant status loaded for Event {EventId}, Project {ProjectId}, User {UserId}. EventStatus={EventStatus} EligibilityStatus={EligibilityStatus} CanValidate={CanValidate}",
            request.EventId,
            request.ProjectId,
            request.UserId,
            effectiveEventStatus,
            eligibility.Status,
            canValidate);

        return new EventParticipantStatusDto
        {
            EventId = eventEntity.Id,
            ProjectId = request.ProjectId,
            EventName = eventEntity.Name,
            ProjectTitle = project.Title ?? "Projeto",
            EventStatus = effectiveEventStatus,
            IsEventActive = string.Equals(effectiveEventStatus, EventStatusActive, StringComparison.OrdinalIgnoreCase),
            IsEventClosed = IsClosedLikeStatus(effectiveEventStatus),
            EventStartsAtUtc = eventEntity.StartsAtUtc,
            EventEndsAtUtc = eventEntity.EndsAtUtc,
            ValidationWindowStartsAtUtc = validationWindowStartsAtUtc,
            ValidationWindowEndsAtUtc = validationWindowEndsAtUtc,
            IsValidationWindowOpen = isValidationWindowOpen,
            TargetWords = progressMetrics.TargetWords,
            TotalWords = progressMetrics.TotalWords,
            Percent = progressMetrics.Percent,
            RemainingWords = progressMetrics.RemainingWords,
            IsValidated = isValidated,
            IsWinner = projectEvent.Won,
            IsEligible = eligibility.IsEligible,
            CanValidate = canValidate,
            EligibilityStatus = eligibility.Status,
            EligibilityMessage = eligibility.Message,
            ValidationBlockReason = validationBlockReason,
            ValidatedAtUtc = projectEvent.ValidatedAtUtc,
            ValidatedWords = projectEvent.ValidatedWords,
            ValidationSource = projectEvent.ValidationSource,
            AllowedValidationSources = allowedSources
        };
    }

    private async Task<int> GetTotalWordsInEventWindowAsync(
        Guid projectId,
        Guid userId,
        EventDto eventEntity,
        CancellationToken cancellationToken)
    {
        var progress = await projectProgressReadRepository.GetProgressByProjectIdAsync(projectId, userId, cancellationToken);
        var endExclusive = eventProgressCalculator.ResolveWindowEndExclusive(eventEntity.EndsAtUtc);

        return progress
            .Where(x => x.CreatedAt >= eventEntity.StartsAtUtc && x.CreatedAt < endExclusive)
            .Sum(x => (int?)x.WordsWritten) ?? 0;
    }

    private static int ResolveEffectiveTotalWords(int totalWordsInWindow, ProjectEvent projectEvent, string effectiveEventStatus)
    {
        var persistedSnapshotWords = projectEvent.ValidatedWords ?? projectEvent.FinalWordCount;
        if (persistedSnapshotWords is null)
            return totalWordsInWindow;

        if (projectEvent.ValidatedAtUtc.HasValue && IsClosedLikeStatus(effectiveEventStatus))
            return Math.Max(0, persistedSnapshotWords.Value);

        return Math.Max(totalWordsInWindow, persistedSnapshotWords.Value);
    }

    private static string? ResolveValidationBlockReason(
        bool isValidated,
        bool isValidationWindowOpen,
        int totalWords,
        int targetWords)
    {
        if (isValidated)
            return "Projeto já validado neste evento.";

        if (!isValidationWindowOpen)
            return "Validação fora da janela permitida.";

        if (totalWords < targetWords)
            return $"Faltam {targetWords - totalWords} palavras para atingir a meta.";

        return null;
    }

    private static string ResolveEffectiveEventStatus(DateTime nowUtc, bool isEventActive, DateTime startsAtUtc, DateTime endsAtUtc)
    {
        if (!isEventActive)
            return EventStatusDisabled;

        if (nowUtc < startsAtUtc)
            return EventStatusScheduled;

        if (nowUtc > endsAtUtc)
            return EventStatusClosed;

        return EventStatusActive;
    }

    private static bool IsClosedLikeStatus(string effectiveEventStatus) =>
        string.Equals(effectiveEventStatus, EventStatusClosed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(effectiveEventStatus, EventStatusDisabled, StringComparison.OrdinalIgnoreCase);
}
