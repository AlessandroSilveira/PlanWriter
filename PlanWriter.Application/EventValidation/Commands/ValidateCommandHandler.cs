using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
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
    IEventReadRepository eventReadRepository) : IRequestHandler<ValidateCommand, Unit>
{
    public async Task<Unit> Handle(ValidateCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Validating project {ProjectId} for event {EventId}", request.ProjectId, request.EventId);

        var (eventEntity, projectEvent) = await LoadContextAsync(request.CurrentUserId, request.EventId, request.ProjectId, cancellationToken);

        var targetWords = projectEvent.TargetWords ?? eventEntity.DefaultTargetWords ?? 50000;

        if (request.Words < targetWords)
        {
            throw new InvalidOperationException($"Total informado ({request.Words}) é menor que a meta ({targetWords}).");
        }

        projectEvent.Won = true;
        projectEvent.ValidatedAtUtc = DateTime.UtcNow;
        projectEvent.ValidatedWords = request.Words;
        projectEvent.ValidationSource = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source;

        await projectEventsRepository.UpdateProjectEvent(projectEvent, cancellationToken);

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
}
