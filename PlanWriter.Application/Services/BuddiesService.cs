using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Buddies;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.Application.Services;

public class BuddiesService(
    IUserFollowRepository userFollowRepo,
    IBuddiesRepository repo,
    IProjectProgressRepository progressRepo,
    IUserRepository userRepo)
    : IBuddiesService
{
    public async Task FollowByUsernameAsync(Guid me, string email, CancellationToken ct)
    {
        var followeeId = await repo.FindUserIdByUsernameAsync(email, ct)
                         ?? throw new KeyNotFoundException("Usuário não encontrado.");

        await FollowByIdAsync(me, followeeId, ct);
    }

    public async Task FollowByIdAsync(Guid me, Guid followeeId, CancellationToken ct)
    {
        if (me == followeeId)
            throw new InvalidOperationException("Você não pode seguir a si mesmo.");

        if (await userFollowRepo.ExistsAsync(me, followeeId, ct))
            return; // idempotente

        await userFollowRepo.AddAsync(new UserFollow { FollowerId = me, FolloweeId = followeeId }, ct);
    }

    public Task UnfollowAsync(Guid me, Guid followeeId, CancellationToken ct) =>
        userFollowRepo.RemoveAsync(me, followeeId, ct);

    public async Task<List<BuddiesDto.BuddySummaryDto>> ListAsync(Guid me, CancellationToken ct)
    {
        var ids = await userFollowRepo.GetFolloweeIdsAsync(me, ct);
        return await repo.GetBuddySummariesAsync(ids, ct);
    }

    public async Task<List<BuddiesDto.BuddyLeaderboardItemDto>> LeaderboardAsync(Guid me, Guid? eventId, DateOnly? start, DateOnly? end, CancellationToken ct)
    {
        // Ids a considerar: meus buddies + eu
        var followeeIds = await userFollowRepo.GetFolloweeIdsAsync(me, ct);
        var userIds = followeeIds.Append(me).Distinct().ToList();

        // Intervalo do evento (se informado)
        if (eventId.HasValue)
        {
            var window = await repo.GetEventWindowAsync(eventId.Value, ct)
                         ?? throw new KeyNotFoundException("Evento não encontrado.");

            start ??= window.start;
            end   ??= window.end;
        }

        // Totais por usuário
        var totals = await repo.GetTotalsAsync(userIds, start, end, ct);
        var myTotal = totals.TryGetValue(me, out var mine) ? mine : 0;

        // Summaries para nomes/avatars
        var summaries = await repo.GetBuddySummariesAsync(userIds, ct);
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
    
    public async Task<List<BuddyLeaderboardRowDto>> GetBuddiesLeaderboardAsync(Guid userId, DateTime? start, DateTime? end)
    {
        // 🔑 default inteligente
        var startDate = start?.Date ?? DateTime.MinValue;
        var endDate   = end?.Date   ?? DateTime.MaxValue;
        
        // 1️⃣ Quem eu sigo
        var buddyIds = await userFollowRepo.GetFolloweeIdsAsync(userId, new CancellationToken());

        // inclui o próprio usuário
        if (!buddyIds.Contains(userId))
            buddyIds.Add(userId);

        // 2️⃣ Totais por usuário (ProjectProgress)
        var totals = await progressRepo.GetTotalWordsByUsersAsync(buddyIds, startDate, endDate);

        var myTotal = totals.GetValueOrDefault(userId, 0);

        // 3️⃣ Dados dos usuários
        var users = await userRepo.GetUsersByIdsAsync(buddyIds);

        // 4️⃣ Resultado final
        
        var result =  users
            .Select(u =>
            {
                var total = totals.GetValueOrDefault(u.Id, 0);

                return new BuddyLeaderboardRowDto
                {
                    UserId = u.Id,
                    Username = u.FirstName + " " + u.LastName,
                    DisplayName = u.DisplayName,
                    Total = total,
                    PaceDelta = total - myTotal,
                    IsMe = u.Id == userId
                };
            })
            .OrderByDescending(x => x.Total)
            .ToList();
        
        return result;
    }

}

