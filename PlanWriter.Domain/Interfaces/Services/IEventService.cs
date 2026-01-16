using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Requests;
using CreateEventRequest = PlanWriter.Domain.Requests.CreateEventRequest;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IEventService
{
    Task<EventDto[]> GetActiveAsync();
    Task<EventDto>   CreateAsync(CreateEventRequest req);

    // ✅ permanecem
    Task<ProjectEvent> JoinAsync(JoinEventRequest req);
    Task<EventProgressDto> GetProgressAsync(Guid projectId, Guid eventId);
    Task<ProjectEvent> FinalizeAsync(Guid projectEventId);
    Task<List<EventLeaderboardRowDto>> GetLeaberBoard(Guid eventId, string scope, int top);

    // ✅ novos
    Task<EventDto?> GetByIdAsync(Guid eventId);
    Task LeaveAsync(Guid projectId, Guid eventId);
    Task<List<EventDto>?> GetAllAsync();
    Task UpdateAsync(Guid id, UpdateEventDto dto);
    Task DeleteAsync(Guid id);
    Task<List<MyEventDto>> GetMyEventsAsync(Guid userId);
}