using System;
using System.Threading.Tasks;
using PlanWriter.Domain.Events;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectEventsRepository
{
    Task<ProjectEvent?> GetProjectEventByProjectIdAndEventId(Guid reqProjectId, Guid reqEventId);
    Task<ProjectEvent> AddProjectEvent(ProjectEvent pe);
    Task<ProjectEvent?> GetProjectEventByProjectId(Guid projectEventId);
    Task<ProjectEvent?> GetProjectEventById(Guid projectEventId);

    // ✅ novo: atualizar inscrição (ex.: mudar TargetWords)
    Task UpdateProjectEvent(ProjectEvent entity);

    // ✅ novo: remover inscrição por (ProjectId, EventId)
    Task<bool> RemoveByKeys(Guid projectId, Guid eventId);

    
}