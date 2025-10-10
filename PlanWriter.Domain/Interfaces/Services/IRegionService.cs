// PlanWriter.Application/Services/RegionsService.cs

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IRegionsService
{
    Task<IEnumerable<RegionLeaderboardDto>> GetLeaderboardAsync(CancellationToken ct);
}