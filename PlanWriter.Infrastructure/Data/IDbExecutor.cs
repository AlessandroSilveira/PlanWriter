using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Infrastructure.Data;

public interface IDbExecutor
{
    Task<int> ExecuteAsync(
        IDbConnection connection,
        string sql,
        object? param,
        IDbTransaction? transaction,
        CancellationToken ct);
}