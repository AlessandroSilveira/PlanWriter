// PlanWriter.Domain/Interfaces/IBuddiesRepository.cs

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Buddies;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IBuddiesRepository
{
    // Users
    Task<Guid?> FindUserIdByUsernameAsync(string username, CancellationToken ct);
    Task<List<BuddiesDto.BuddySummaryDto>> GetBuddySummariesAsync(IEnumerable<Guid> userIds, CancellationToken ct);

    // Events
    Task<(DateOnly start, DateOnly end)?> GetEventWindowAsync(Guid eventId, CancellationToken ct);

    // Progress totals (words) por usuário no intervalo
    Task<Dictionary<Guid, int>> GetTotalsAsync(IEnumerable<Guid> userIds, DateOnly? start, DateOnly? end, CancellationToken ct);
    Task<List<BuddiesDto.BuddySummaryDto>> GetBuddies(Guid userId);
    
    Task<List<BuddiesDto.BuddySummaryDto>> GetByUserId(Guid userId);
}