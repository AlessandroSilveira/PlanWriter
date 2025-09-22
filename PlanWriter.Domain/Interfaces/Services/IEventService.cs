using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Events;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IEventService
{
    Task<EventDto[]> GetActiveAsync();
    Task<EventDto>   CreateAsync(CreateEventRequest req);
    Task<ProjectEvent> JoinAsync(JoinEventRequest req);
    Task<EventProgressDto> GetProgressAsync(Guid projectId, Guid eventId);
    Task<ProjectEvent> FinalizeAsync(Guid projectEventId);
    Task<List<EventLeaderboardRowDto>>GetLeaberBoard(Guid eventId, string scope, int top);
}