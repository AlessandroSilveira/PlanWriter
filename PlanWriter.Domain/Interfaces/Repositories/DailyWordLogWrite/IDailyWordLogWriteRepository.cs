using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Domain.Interfaces.Repositories.DailyWordLogWrite;

public interface IDailyWordLogWriteRepository
{
    Task UpsertAsync(Guid projectId, Guid userId, DateOnly date, int wordsWritten, CancellationToken ct);
    Task<IReadOnlyList<DailyWordLogDto>> GetByProjectAsync(Guid projectId, Guid userId, CancellationToken ct);
}
