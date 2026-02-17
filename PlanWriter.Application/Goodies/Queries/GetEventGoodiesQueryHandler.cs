using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Common.WinnerEligibility;
using PlanWriter.Application.Goodies.Dtos.Queries;
using PlanWriter.Domain.Dtos.Badges;
using PlanWriter.Domain.Dtos.Goodies;
using PlanWriter.Domain.Interfaces.ReadModels.Badges;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using IEventReadRepository = PlanWriter.Domain.Interfaces.ReadModels.Events.IEventReadRepository;

namespace PlanWriter.Application.Goodies.Queries;

public sealed class GetEventGoodiesQueryHandler(
    ILogger<GetEventGoodiesQueryHandler> logger,
    IEventReadRepository eventReadRepository,
    IProjectReadRepository projectReadRepository,
    IProjectEventsReadRepository projectEventsReadRepository,
    IProjectProgressReadRepository projectProgressReadRepository,
    IBadgeReadRepository badgeReadRepository,
    IWinnerEligibilityService winnerEligibilityService)
    : IRequestHandler<GetEventGoodiesQuery, EventGoodiesDto>
{
    public async Task<EventGoodiesDto> Handle(GetEventGoodiesQuery request, CancellationToken cancellationToken)
    {
        var eventEntity = await eventReadRepository.GetEventByIdAsync(request.EventId, cancellationToken)
                          ?? throw new NotFoundException("Evento não encontrado.");

        var project = await projectReadRepository.GetUserProjectByIdAsync(
                          request.ProjectId,
                          request.UserId,
                          cancellationToken)
                      ?? throw new NotFoundException("Projeto não encontrado.");

        var projectEvent = await projectEventsReadRepository.GetByProjectAndEventWithEventAsync(
                               request.ProjectId,
                               request.EventId,
                               cancellationToken)
                           ?? throw new NotFoundException("Projeto não está inscrito neste evento.");

        var targetWords = projectEvent.TargetWords ?? eventEntity.DefaultTargetWords ?? 50000;
        var totalWords = await GetTotalWordsInEventWindowAsync(
            request.ProjectId,
            request.UserId,
            eventEntity.StartsAtUtc,
            eventEntity.EndsAtUtc,
            cancellationToken);

        var persistedTotal = projectEvent.ValidatedWords ?? projectEvent.FinalWordCount ?? 0;
        var effectiveTotalWords = Math.Max(totalWords, persistedTotal);

        var eligibility = winnerEligibilityService.EvaluateForGoodies(
            DateTime.UtcNow,
            eventEntity.EndsAtUtc,
            targetWords,
            effectiveTotalWords,
            projectEvent.ValidatedAtUtc.HasValue,
            projectEvent.Won);

        var badges = await badgeReadRepository.GetByProjectIdAsync(request.ProjectId, request.UserId, cancellationToken);
        var eventBadges = badges
            .Where(x => x.EventId == request.EventId)
            .Select(x => new BadgeDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                EventId = x.EventId,
                Name = x.Name,
                Description = x.Description,
                Icon = x.Icon,
                AwardedAt = x.AwardedAt
            })
            .ToList();

        logger.LogInformation(
            "Goodies loaded for Event {EventId}, Project {ProjectId}, User {UserId}. Eligible={Eligible}, Status={Status}",
            request.EventId,
            request.ProjectId,
            request.UserId,
            eligibility.IsEligible,
            eligibility.Status);

        return new EventGoodiesDto
        {
            EventId = request.EventId,
            ProjectId = request.ProjectId,
            EventName = eventEntity.Name,
            ProjectTitle = project.Title ?? "Projeto",
            TargetWords = targetWords,
            TotalWords = effectiveTotalWords,
            ValidatedAtUtc = projectEvent.ValidatedAtUtc,
            Won = projectEvent.Won,
            Eligibility = new WinnerEligibilityDto
            {
                IsEligible = eligibility.IsEligible,
                CanValidate = eligibility.CanValidate,
                Status = eligibility.Status,
                Message = eligibility.Message
            },
            Certificate = new CertificateGoodieDto
            {
                Available = eligibility.IsEligible,
                DownloadUrl = eligibility.IsEligible
                    ? $"/api/events/{request.EventId}/projects/{request.ProjectId}/certificate"
                    : null,
                Message = eligibility.IsEligible
                    ? "Certificado disponível para download."
                    : eligibility.CanValidate
                        ? "Faça a validação final para liberar o certificado."
                        : eligibility.Message
            },
            Badges = eventBadges
        };
    }

    private async Task<int> GetTotalWordsInEventWindowAsync(
        Guid projectId,
        Guid userId,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        CancellationToken cancellationToken)
    {
        var progress = await projectProgressReadRepository.GetProgressByProjectIdAsync(projectId, userId, cancellationToken);
        return progress
            .Where(x => x.CreatedAt >= startsAtUtc && x.CreatedAt < endsAtUtc)
            .Sum(x => (int?)x.WordsWritten) ?? 0;
    }
}
