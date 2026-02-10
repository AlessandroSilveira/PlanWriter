using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Commands;

public class JoinEventCommandHandler(
    IEventRepository eventRepository,                     // read (event)
    IProjectRepository projectRepository,                  // read (ownership)
    IProjectEventsRepository projectEventsRepository,      // write
    IProjectEventsReadRepository projectEventsReadRepository, // read
    ILogger<JoinEventCommandHandler> logger,
    IEventReadRepository eventReadRepository,
    IProjectReadRepository projectReadRepository
) : IRequestHandler<JoinEventCommand, ProjectEvent>
{
    public async Task<ProjectEvent> Handle(JoinEventCommand request, CancellationToken ct)
    {
        logger.LogInformation("Joining event {EventId} with project {ProjectId}", request.Req.EventId, request.Req.ProjectId);
       
        var ev = await eventReadRepository.GetEventByIdAsync(request.Req.EventId, ct)
                 ?? throw new KeyNotFoundException("Evento não encontrado.");

        var project = await projectReadRepository.GetProjectByIdAsync(request.Req.ProjectId, request.UserId, ct)
                      ?? throw new KeyNotFoundException("Projeto não encontrado.");

        
        var existing = await projectEventsReadRepository
            .GetByProjectAndEventWithEventAsync(request.Req.ProjectId, request.Req.EventId, ct);

        if (existing is not null)
        {
            await UpdateTargetWordsIfNeeded(existing, request, ct);
            return existing;
        }
        
        var created = await CreateProjectEventAsync(request, ev, ct);

        logger.LogInformation("Project {ProjectId} joined event {EventId} successfully", request.Req.ProjectId, request.Req.EventId);

        return created;
    }

    private async Task UpdateTargetWordsIfNeeded(ProjectEvent existing, JoinEventCommand request, CancellationToken ct)
    {
        if (!request.Req.TargetWords.HasValue)
            return;

        if (existing.TargetWords == request.Req.TargetWords.Value)
            return;

        logger.LogInformation(
            "Updating target words for project {ProjectId} in event {EventId}: {Old} → {New}",
            existing.ProjectId,
            existing.EventId,
            existing.TargetWords,
            request.Req.TargetWords.Value
        );

        await projectEventsRepository.UpdateTargetWordsAsync(existing.Id, request.Req.TargetWords.Value, ct);
        
        existing.TargetWords = request.Req.TargetWords.Value;
    }

    private async Task<ProjectEvent> CreateProjectEventAsync(JoinEventCommand request, EventDto ev, CancellationToken ct)
    {
        var pe = new ProjectEvent
        {
            Id          = Guid.NewGuid(),
            ProjectId   = request.Req.ProjectId,
            EventId     = request.Req.EventId,
            TargetWords = request.Req.TargetWords ?? ev.DefaultTargetWords
        };

        await projectEventsRepository.CreateAsync(pe, ct);

        return pe;
    }
}