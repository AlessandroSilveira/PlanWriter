using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Domain.Interfaces.Repositories;

// PlanWriter.Domain/Interfaces/IRegionsRepository.cs
public interface IRegionsRepository
{
    Task<IEnumerable<(string regionName, Guid regionId, int totalWords, int userCount)>> GetLeaderboardAsync(CancellationToken ct);
}
