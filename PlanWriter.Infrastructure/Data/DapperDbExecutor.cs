using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace PlanWriter.Infrastructure.Data;

public sealed class DapperDbExecutor(IDbConnectionFactory connectionFactory) : IDbExecutor
{
    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken ct = default)
    {
        using var conn = connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var result = await conn.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken ct = default)
    {
        using var conn = connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct));
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken ct = default)
    {
        using var conn = connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: ct));
    }
}