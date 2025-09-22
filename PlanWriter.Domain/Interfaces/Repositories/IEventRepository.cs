using System;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Events;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IEventRepository
{
    Task<EventDto[]> GetActiveEvents();
    Task<bool> GetEventBySlug(string reqSlug);
    Task AddEvent(Event ev);
    Task<Event?> GetEventById(Guid reqEventId);
}