using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Infrastructure.Data;

public interface IDbExecutor
{
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken ct = default);

    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken ct = default);

    Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken ct = default);
}
