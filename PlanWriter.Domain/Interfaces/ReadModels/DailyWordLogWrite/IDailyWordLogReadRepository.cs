using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Domain.Interfaces.ReadModels.DailyWordLogWrite;

public interface IDailyWordLogReadRepository
{
    Task<IReadOnlyList<DailyWordLogDto>> GetByProjectAsync(Guid projectId, Guid userId, CancellationToken ct);
}