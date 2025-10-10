// PlanWriter.Application/Services/BuddiesService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;

// IUserFollowRepository + IBuddiesRepository


namespace PlanWriter.Application.Services;

public class BuddiesService : IBuddiesService
{
    private readonly IUserFollowRepository _follows;
    private readonly IBuddiesRepository _repo;

    public BuddiesService(IUserFollowRepository follows, IBuddiesRepository repo)
    {
        _follows = follows;
        _repo = repo;
    }

    public async Task FollowByUsernameAsync(Guid me, string username, CancellationToken ct)
    {
        var followeeId = await _repo.FindUserIdByUsernameAsync(username, ct)
                         ?? throw new KeyNotFoundException("Usuário não encontrado.");

        await FollowByIdAsync(me, followeeId, ct);
    }

    public async Task FollowByIdAsync(Guid me, Guid followeeId, CancellationToken ct)
    {
        if (me == followeeId)
            throw new InvalidOperationException("Você não pode seguir a si mesmo.");

        if (await _follows.ExistsAsync(me, followeeId, ct))
            return; // idempotente

        await _follows.AddAsync(new UserFollow { FollowerId = me, FolloweeId = followeeId }, ct);
    }

    public Task UnfollowAsync(Guid me, Guid followeeId, CancellationToken ct) =>
        _follows.RemoveAsync(me, followeeId, ct);

    public async Task<List<BuddiesDto.BuddySummaryDto>> ListAsync(Guid me, CancellationToken ct)
    {
        var ids = await _follows.GetFolloweeIdsAsync(me, ct);
        return await _repo.GetBuddySummariesAsync(ids, ct);
    }

    public async Task<List<BuddiesDto.BuddyLeaderboardItemDto>> LeaderboardAsync(Guid me, Guid? eventId, DateOnly? start, DateOnly? end, CancellationToken ct)
    {
        // Ids a considerar: meus buddies + eu
        var followeeIds = await _follows.GetFolloweeIdsAsync(me, ct);
        var userIds = followeeIds.Append(me).Distinct().ToList();

        // Intervalo do evento (se informado)
        if (eventId.HasValue)
        {
            var window = await _repo.GetEventWindowAsync(eventId.Value, ct)
                         ?? throw new KeyNotFoundException("Evento não encontrado.");

            start ??= window.start;
            end   ??= window.end;
        }

        // Totais por usuário
        var totals = await _repo.GetTotalsAsync(userIds, start, end, ct);
        var myTotal = totals.TryGetValue(me, out var mine) ? mine : 0;

        // Summaries para nomes/avatars
        var summaries = await _repo.GetBuddySummariesAsync(userIds, ct);
        var map = summaries.ToDictionary(s => s.UserId, s => s);

        // Monta ranking
        var list = new List<BuddiesDto.BuddyLeaderboardItemDto>(userIds.Count);
        foreach (var id in userIds)
        {
            var sum = map.GetValueOrDefault(id);
            var total = totals.GetValueOrDefault(id, 0);
            var delta = id == me ? 0 : total - myTotal;

            list.Add(new BuddiesDto.BuddyLeaderboardItemDto(
                id,
                sum?.Username ?? id.ToString("N"),
                sum?.DisplayName ?? (sum?.Username ?? id.ToString("N")),
                total,
                delta
            ));
        }

        return list.OrderByDescending(x => x.Total).ToList();
    }
}

