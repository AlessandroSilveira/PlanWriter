using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace PlanWriter.Infrastructure.Data;

public sealed class DapperDbExecutor : IDbExecutor
{
    public Task<int> ExecuteAsync(
        IDbConnection connection,
        string sql,
        object? param,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        var cmd = new CommandDefinition(sql, param, transaction, cancellationToken: ct);
        return connection.ExecuteAsync(cmd);
    }
}