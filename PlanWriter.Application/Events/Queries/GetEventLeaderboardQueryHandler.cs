using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Events.Queries;

public class GetEventLeaderboardQueryHandler(IEventRepository eventRepository, ILogger<GetEventLeaderboardQueryHandler> logger)
    : IRequestHandler<GetEventLeaderboardQuery, List<EventLeaderboardRowDto>>
{
    public async Task<List<EventLeaderboardRowDto>> Handle(GetEventLeaderboardQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting leaderboard for event {EventId} with scope {Scope}", request.EventId, request.Scope);

        var eventEntity = await eventRepository.GetEventById(request.EventId)
            ?? throw new KeyNotFoundException("Evento não encontrado.");

        var leaderboardWindow = ResolveLeaderboardWindow(eventEntity, request.Scope);

        if (leaderboardWindow.IsEmpty)
        {
            logger.LogInformation("Leaderboard window is empty for event {EventId}", request.EventId);
            return [];
        }

        var rawLeaderboardRows = await eventRepository.GetLeaderboard(eventEntity, leaderboardWindow.Start,
            leaderboardWindow.End, request.Top);

        var rankedRows = ApplyRankingAndOrdering(rawLeaderboardRows, request.Top);

        logger.LogInformation("Returning {Count} leaderboard rows for event {EventId}", rankedRows.Count, request.EventId);

        return rankedRows;
    }
    

    private static LeaderboardWindow ResolveLeaderboardWindow(Domain.Events.Event eventEntity, string scope)
    {
        var eventStartDate = eventEntity.StartsAtUtc.Date;
        var eventEndDate = eventEntity.EndsAtUtc.Date;
        var today = DateTime.UtcNow.Date;

        var effectiveEndDate = today < eventEndDate ? today : eventEndDate;

        if (string.Equals(scope, "daily", StringComparison.OrdinalIgnoreCase))
        {
            if (today < eventStartDate || today > eventEndDate)
                return LeaderboardWindow.Empty();

            return LeaderboardWindow.ForSingleDay(today);
        }

        return LeaderboardWindow.ForRange(eventStartDate, effectiveEndDate);
    }

    private static List<EventLeaderboardRowDto> ApplyRankingAndOrdering(IEnumerable<EventLeaderboardRowDto> rows, int top)
    {
        var limited = rows
            .OrderByDescending(r => r.Words)
            .ThenBy(r => r.ProjectTitle)
            .Take(Math.Clamp(top, 1, 200))
            .ToList();

        for (var index = 0; index < limited.Count; index++)
            limited[index].Rank = index + 1;

        return limited;
    }

    private readonly record struct LeaderboardWindow(DateTime Start, DateTime End, bool IsEmpty)
    {
        public static LeaderboardWindow Empty()
            => new(DateTime.MinValue, DateTime.MinValue, true);

        public static LeaderboardWindow ForSingleDay(DateTime day)
            => new(day, day, false);

        public static LeaderboardWindow ForRange(DateTime start, DateTime end)
            => new(start, end, false);
    }
}
