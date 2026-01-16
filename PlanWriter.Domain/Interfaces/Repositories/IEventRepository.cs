using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IEventRepository
{
    Task<EventDto[]> GetActiveEvents();
    Task<bool> GetEventBySlug(string reqSlug);
    Task AddEvent(Event ev);
    Task<Event?> GetEventById(Guid reqEventId);
    Task<List<EventDto>?> GetAllAsync();
    Task UpdateAsync(Event ev, Guid id);
    Task DeleteAsync(Event ev);
    Task<List<MyEventDto>> GetEventByUserId(Guid userId);
    
    Task<List<EventLeaderboardRowDto>> GetLeaderboard(Event ev, DateTime winStart, DateTime winEnd, int top);
}