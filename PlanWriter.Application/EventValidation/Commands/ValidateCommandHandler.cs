using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.EventValidation.Dtos.Commands;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.EventValidation.Commands;

public class ValidateCommandHandler(ILogger<ValidateCommandHandler> logger, IEventRepository eventRepository,
    IProjectRepository projectRepository, IProjectEventsRepository projectEventsRepository) : IRequestHandler<ValidateCommand, Unit>
{
    public async Task<Unit> Handle(ValidateCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Validating project {ProjectId} for event {EventId}", request.ProjectId, request.EventId);

        var (eventEntity, projectEvent) = await LoadContextAsync(request.CurrentUserId, request.EventId, request.ProjectId);

        var targetWords = projectEvent.TargetWords ?? eventEntity.DefaultTargetWords ?? 50000;

        if (request.Words < targetWords)
        {
            throw new InvalidOperationException($"Total informado ({request.Words}) é menor que a meta ({targetWords}).");
        }

        projectEvent.Won = true;
        projectEvent.ValidatedAtUtc = DateTime.UtcNow;
        projectEvent.ValidatedWords = request.Words;
        projectEvent.ValidationSource = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source;

        await projectEventsRepository.UpdateProjectEvent(projectEvent);

        logger.LogInformation("ProjectEvent {ProjectEventId} validated successfully", projectEvent.Id);

        return Unit.Value;
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
}
