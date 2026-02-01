using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Commands;

public class JoinEventCommandHandler(IEventRepository eventRepository, IProjectRepository projectRepository,
    IProjectEventsRepository projectEventsRepository, ILogger<JoinEventCommandHandler> logger)
    : IRequestHandler<JoinEventCommand, ProjectEvent>
{
    public async Task<ProjectEvent> Handle(JoinEventCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("User joining event {EventId} with project {ProjectId}", request.Req.EventId, request.Req.ProjectId
        );

        var ev = await eventRepository.GetEventById(request.Req.EventId)
                 ?? throw new KeyNotFoundException("Evento não encontrado.");

        var project = await projectRepository.GetProjectById(request.Req.ProjectId)
                      ?? throw new KeyNotFoundException("Projeto não encontrado.");

        var existing = await projectEventsRepository
            .GetProjectEventByProjectIdAndEventId(
                request.Req.ProjectId,
                request.Req.EventId
            );

        if (existing is not null)
        {
            logger.LogInformation("Project {ProjectId} already joined event {EventId}", request.Req.ProjectId, request.Req.EventId);

            await UpdateTargetWordsIfNeeded(existing, request, ev);

            return existing;
        }

        var created = await CreateProjectEvent(request, ev);

        logger.LogInformation("Project {ProjectId} successfully joined event {EventId}", request.Req.ProjectId, request.Req.EventId);

        return created;
    }

 

    private async Task UpdateTargetWordsIfNeeded(ProjectEvent existing, JoinEventCommand request, Event ev)
    {
        if (request.Req.TargetWords.HasValue &&
            existing.TargetWords != request.Req.TargetWords.Value)
        {
            logger.LogInformation(
                "Updating target words for project {ProjectId} in event {EventId} from {OldTarget} to {NewTarget}",
                existing.ProjectId,
                existing.EventId,
                existing.TargetWords,
                request.Req.TargetWords.Value
            );

            existing.TargetWords = request.Req.TargetWords.Value;
            await projectEventsRepository.UpdateProjectEvent(existing);
        }
    }

    private Task<ProjectEvent> CreateProjectEvent(JoinEventCommand request, Event ev)
    {
        var pe = new ProjectEvent
        {
            ProjectId = request.Req.ProjectId,
            EventId = request.Req.EventId,
            TargetWords = request.Req.TargetWords ?? ev.DefaultTargetWords
        };

        return projectEventsRepository.AddProjectEvent(pe);
    }
}
