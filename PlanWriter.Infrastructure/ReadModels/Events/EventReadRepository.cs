using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Events;

public class EventReadRepository(IDbExecutor db) : IEventReadRepository
{
    public Task<IReadOnlyList<EventDto>> GetActiveAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                Id,
                Name,
                Description,
                StartDate,
                EndDate,
                IsActive
            FROM Events
            WHERE IsActive = 1
              AND StartDate <= SYSUTCDATETIME()
              AND EndDate >= SYSUTCDATETIME()
            ORDER BY StartDate;
        ";

        return db.QueryAsync<EventDto>(sql, param: null, ct: cancellationToken);
    }
}