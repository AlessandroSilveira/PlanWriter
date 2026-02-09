using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Buddies;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class BuddiesRepository(IDbExecutor db) : IBuddiesRepository
{
    private sealed record BuddyUserRow(
        Guid Id,
        string? Slug,
        string? Email,
        string? FirstName,
        string? LastName,
        string? DisplayName,
        string? AvatarUrl);

    private sealed record EventWindowRow(DateTime StartsAtUtc, DateTime EndsAtUtc);

    public Task<Guid?> FindUserIdByUsernameAsync(string username, CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP 1 Id
            FROM Users
            WHERE Slug = @Username
               OR Email = @Username;
        ";

        return db.QueryFirstOrDefaultAsync<Guid?>(sql, new { Username = username }, ct);
    }

    public async Task<List<BuddiesDto.BuddySummaryDto>> GetBuddySummariesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        const string sql = @"
            SELECT
                Id,
                Slug,
                Email,
                FirstName,
                LastName,
                DisplayName,
                AvatarUrl
            FROM Users
            WHERE Id IN @Ids;
        ";

        var rows = await db.QueryAsync<BuddyUserRow>(sql, new { Ids = ids }, ct);

        return rows
            .Select(u =>
            {
                var username = u.Slug
                               ?? u.Email
                               ?? $"{u.FirstName}-{u.LastName}";
                var displayName = u.DisplayName
                                  ?? $"{u.FirstName} {u.LastName}";

                return new BuddiesDto.BuddySummaryDto(
                    u.Id,
                    username,
                    displayName,
                    u.AvatarUrl
                );
            })
            .ToList();
    }

    public async Task<(DateOnly start, DateOnly end)?> GetEventWindowAsync(Guid eventId, CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP 1
                StartsAtUtc,
                EndsAtUtc
            FROM Events
            WHERE Id = @Id;
        ";

        var ev = await db.QueryFirstOrDefaultAsync<EventWindowRow>(sql, new { Id = eventId }, ct);
        if (ev is null)
            return null;

        return (DateOnly.FromDateTime(ev.StartsAtUtc), DateOnly.FromDateTime(ev.EndsAtUtc));
    }

    public async Task<Dictionary<Guid, int>> GetTotalsAsync(IEnumerable<Guid> userIds, DateOnly? start, DateOnly? end, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, int>();

        var startDate = start?.ToDateTime(TimeOnly.MinValue);
        var endDate = end?.ToDateTime(TimeOnly.MinValue);

        const string sql = @"
            SELECT
                p.UserId AS UserId,
                SUM(pp.WordsWritten) AS Total
            FROM ProjectProgresses pp
            INNER JOIN Projects p ON p.Id = pp.ProjectId
            WHERE p.UserId IN @UserIds
              AND (@Start IS NULL OR pp.[Date] >= @Start)
              AND (@End IS NULL OR pp.[Date] <= @End)
            GROUP BY p.UserId;
        ";

        var rows = await db.QueryAsync<(Guid UserId, int Total)>(
            sql,
            new { UserIds = ids, Start = startDate, End = endDate },
            ct
        );

        return rows.ToDictionary(x => x.UserId, x => x.Total);
    }

    public async Task<List<BuddiesDto.BuddySummaryDto>> GetBuddies(Guid userId)
    {
        const string sql = @"
            SELECT
                u.Id,
                u.DisplayName,
                u.FirstName,
                u.LastName,
                u.AvatarUrl
            FROM UserFollows uf
            INNER JOIN Users u ON u.Id = uf.FolloweeId
            WHERE uf.FollowerId = @UserId;
        ";

        var rows = await db.QueryAsync<BuddyUserRow>(sql, new { UserId = userId });

        return rows.Select(u => new BuddiesDto.BuddySummaryDto(
            u.Id,
            u.DisplayName ?? string.Empty,
            $"{u.FirstName} {u.LastName}".Trim(),
            u.AvatarUrl
        )).ToList();
    }

    public async Task<List<BuddiesDto.BuddySummaryDto>> GetByUserId(Guid userId)
    {
        const string sql = @"
            SELECT
                u.Id,
                u.DisplayName,
                u.FirstName,
                u.LastName,
                u.AvatarUrl
            FROM UserFollows uf
            INNER JOIN Users u ON u.Id = uf.FollowerId
            WHERE uf.FollowerId = @UserId;
        ";

        var rows = await db.QueryAsync<BuddyUserRow>(sql, new { UserId = userId });

        return rows.Select(u => new BuddiesDto.BuddySummaryDto(
            u.Id,
            u.DisplayName ?? string.Empty,
            $"{u.FirstName} {u.LastName}".Trim(),
            u.AvatarUrl
        )).ToList();
    }
}
