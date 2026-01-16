using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class BuddiesRepository(AppDbContext db) : IBuddiesRepository
{
    public async Task<Guid?> FindUserIdByUsernameAsync(string username, CancellationToken ct)
    {
        // Ajuste o campo se for, por ex., Login em vez de Username (você não tem Username; usaremos Email ou Slug/DisplayName)
        // Como sua classe User não tem Username, o mais comum é procurar por Email OU Slug.
        // Aqui priorizo Slug; se quiser por Email, troque p/ u.Email == username.
        var id = await db.Users
            .Where(u => u.Slug == username || u.Email == username)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);

        return id;
    }

    public Task<List<BuddiesDto.BuddySummaryDto>> GetBuddySummariesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();

        return db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new BuddiesDto.BuddySummaryDto(
                u.Id,
                // "Username" nos DTOs: usarei Slug se existir, senão Email, senão "FirstName-LastName"
                u.Slug ?? u.Email ?? (u.FirstName + "-" + u.LastName),
                u.DisplayName ?? (u.FirstName + " " + u.LastName),
                u.AvatarUrl
            ))
            .ToListAsync(ct);
    }

    public async Task<(DateOnly start, DateOnly end)?> GetEventWindowAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new { e.StartsAtUtc, e.EndsAtUtc })
            .FirstOrDefaultAsync(ct);

        if (ev is null) return null;

        return (DateOnly.FromDateTime(ev.StartsAtUtc), DateOnly.FromDateTime(ev.EndsAtUtc));
    }

    public async Task<Dictionary<Guid, int>> GetTotalsAsync(IEnumerable<Guid> userIds, DateOnly? start, DateOnly? end, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();

        var query = db.ProjectProgresses.AsQueryable(); // sua DbSet<ProjectProgress>

        if (start.HasValue) query = query.Where(p => p.Date >= Convert.ToDateTime(start.Value) );
        if (end.HasValue)   query = query.Where(p => p.Date <= Convert.ToDateTime(end.Value));

        var rows = await query
            .Where(p => ids.Contains(p.Id))
            .GroupBy(p => p.Id)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.WordsWritten) })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.UserId, x => x.Total);
    }

    public async Task<List<BuddiesDto.BuddySummaryDto>> GetBuddies(Guid userId)
    {
        // quem eu sigo
        var buddies = await db.UserFollows
            .Where(x => x.FollowerId == userId)
            .Join(db.Users, x => x.FolloweeId, u => u.Id, (x, u) => new { x, u })
            .Select(x => new BuddiesDto.BuddySummaryDto
            (
                x.u.Id,
                x.u.DisplayName,
                x.u.FirstName + " " + x.u.LastName,
                x.u.AvatarUrl
            ))
            .ToListAsync();

        return buddies;


    }

    public async Task<List<BuddiesDto.BuddySummaryDto>> GetByUserId(Guid userId)
    {
        // quem eu sigo
        var buddies = await db.UserFollows
            .Where(x => x.FollowerId == userId)
            .Join(db.Users, x => x.FollowerId, u => u.Id, (x, u) => new { x, u })
            .Select(x => new BuddiesDto.BuddySummaryDto
            (
                x.u.Id,
                x.u.DisplayName,
                x.u.FirstName + " " + x.u.LastName,
                x.u.AvatarUrl
            ))
            .ToListAsync();

        return buddies;
    }
}