using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.Application.Services;

public class RegionsService(IRegionsRepository repo) : IRegionsService
{
    public async Task<IEnumerable<RegionLeaderboardDto>> GetLeaderboardAsync(CancellationToken ct)
    {
        var data = await repo.GetLeaderboardAsync(ct);
        return data.Select(x => new RegionLeaderboardDto(x.regionId, x.regionName, x.totalWords, x.userCount));
    }
}