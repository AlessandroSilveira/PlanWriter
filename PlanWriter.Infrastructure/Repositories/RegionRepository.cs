using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class RegionsRepository(AppDbContext db) : IRegionsRepository
{
    public async Task<IEnumerable<(string regionName, Guid regionId, int totalWords, int userCount)>> GetLeaderboardAsync(CancellationToken ct)
    {
        // Palavras totais por região (a partir de ProjectProgresses -> Users -> Regions)
        var totalsByRegionQuery =
            from pp in db.ProjectProgresses
            join u in db.Users on pp.Id equals u.Id
            where u.RegionId != null
            join r in db.Regions on u.RegionId equals r.Id
            group pp by new { r.Id, r.Name } into g
            select new
            {
                RegionId = g.Key.Id,
                RegionName = g.Key.Name,
                TotalWords = g.Sum(x => x.WordsWritten) // <-- se no seu modelo for "Words", troque aqui
            };

        // Contagem de usuários por região
        var usersByRegionQuery =
            from u in db.Users
            where u.RegionId != null
            join r in db.Regions on u.RegionId equals r.Id
            group u by new { r.Id, r.Name } into g
            select new
            {
                RegionId = g.Key.Id,
                RegionName = g.Key.Name,
                UserCount = g.Count()
            };

        // Left join: garante região com usuários mas 0 palavras
        var query =
            from ur in usersByRegionQuery
            join tr in totalsByRegionQuery
                on ur.RegionId equals tr.RegionId into gj
            from tr in gj.DefaultIfEmpty()
            select new
            {
                ur.RegionId,
                ur.RegionName,
                UserCount = ur.UserCount,
                TotalWords = tr != null ? tr.TotalWords : 0
            };

        var rows = await query
            .OrderByDescending(x => x.TotalWords)
            .ToListAsync(ct);

        return rows.Select(x => (x.RegionName, x.RegionId, x.TotalWords, x.UserCount));
    }
}