using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Tests.Infrastructure;

internal sealed class StubDbExecutor : IDbExecutor
{
    public Func<string, object?, CancellationToken, Task<int>>? ExecuteAsyncHandler { get; set; }
    public Func<Type, string, object?, CancellationToken, object?>? QueryAsyncHandler { get; set; }
    public Func<Type, string, object?, CancellationToken, object?>? QueryFirstOrDefaultAsyncHandler { get; set; }

    public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken ct = default)
    {
        var value = QueryAsyncHandler?.Invoke(typeof(T), sql, param, ct);
        if (value is null)
            return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());

        if (value is IReadOnlyList<T> typed)
            return Task.FromResult(typed);

        if (value is IEnumerable enumerable)
            return Task.FromResult<IReadOnlyList<T>>(enumerable.Cast<T>().ToList());

        throw new InvalidOperationException($"Unsupported QueryAsync return type: {value.GetType().Name}");
    }

    public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken ct = default)
    {
        var value = QueryFirstOrDefaultAsyncHandler?.Invoke(typeof(T), sql, param, ct);
        if (value is null)
            return Task.FromResult(default(T));

        if (value is T typed)
            return Task.FromResult<T?>(typed);

        throw new InvalidOperationException(
            $"Unsupported QueryFirstOrDefaultAsync return type. Expected {typeof(T).Name}, got {value.GetType().Name}");
    }

    public Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken ct = default)
        => ExecuteAsyncHandler is null ? Task.FromResult(0) : ExecuteAsyncHandler(sql, param, ct);
}
