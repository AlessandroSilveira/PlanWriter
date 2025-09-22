using System;
using System.Threading.Tasks;
using PlanWriter.Domain.Events;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectEventsRepository
{
    Task<ProjectEvent?> GetProjectEventByProjectIdAndEventId(Guid reqProjectId, Guid reqEventId);
    Task<ProjectEvent> AddProjectEvent(ProjectEvent pe);
    Task<ProjectEvent?> GetProjectEventByProjectId(Guid projectEventId);
}