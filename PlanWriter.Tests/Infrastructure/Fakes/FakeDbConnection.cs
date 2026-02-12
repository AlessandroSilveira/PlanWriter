using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanWriter.Tests.Infrastructure.Fakes;

internal sealed class FakeDbConnection : DbConnection
{
    private string _connectionString = string.Empty;
    private ConnectionState _state = ConnectionState.Closed;

    public int OpenCalls { get; private set; }
    public int BeginTransactionCalls { get; private set; }
    public FakeDbTransaction? LastTransaction { get; private set; }
    public Queue<DataTable> QueryResults { get; } = new();
    public Queue<int> NonQueryResults { get; } = new();
    public Exception? ExecuteNonQueryException { get; set; }

    public override string ConnectionString
    {
        get => _connectionString;
        set => _connectionString = value;
    }

    public override string Database => "Fake";
    public override string DataSource => "Fake";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    public override void Open()
    {
        OpenCalls++;
        _state = ConnectionState.Open;
    }

    public override void Close()
    {
        _state = ConnectionState.Closed;
    }

    public void ForceOpenState() => _state = ConnectionState.Open;

    public override void ChangeDatabase(string databaseName)
    {
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        BeginTransactionCalls++;
        LastTransaction = new FakeDbTransaction(this, isolationLevel);
        return LastTransaction;
    }

    protected override DbCommand CreateDbCommand() => new FakeDbCommand(this);

    internal int NextNonQueryResult()
    {
        if (ExecuteNonQueryException is not null)
            throw ExecuteNonQueryException;

        return NonQueryResults.Count == 0 ? 0 : NonQueryResults.Dequeue();
    }

    internal DbDataReader NextReader()
    {
        var table = QueryResults.Count == 0 ? new DataTable() : QueryResults.Dequeue();
        return table.CreateDataReader();
    }
}

internal sealed class FakeDbTransaction(FakeDbConnection connection, IsolationLevel isolationLevel) : DbTransaction
{
    public bool Committed { get; private set; }
    public bool RolledBack { get; private set; }

    public override IsolationLevel IsolationLevel => isolationLevel;
    protected override DbConnection DbConnection => connection;

    public override void Commit()
    {
        Committed = true;
    }

    public override void Rollback()
    {
        RolledBack = true;
    }
}

internal sealed class FakeDbCommand(FakeDbConnection connection) : DbCommand
{
    private readonly FakeDbParameterCollection _parameters = new();

    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection DbConnection { get; set; } = connection;
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery() => connection.NextNonQueryResult();

    public override object? ExecuteScalar()
    {
        using var reader = connection.NextReader();
        if (!reader.Read())
            return null;

        return reader.FieldCount == 0 ? null : reader.GetValue(0);
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => connection.NextReader();

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<int>(cancellationToken)
            : Task.FromResult(ExecuteNonQuery());

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<DbDataReader>(cancellationToken)
            : Task.FromResult(ExecuteDbDataReader(behavior));
}

internal sealed class FakeDbParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    public override string ParameterName { get; set; } = string.Empty;
    public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }

    public override void ResetDbType()
    {
    }
}

internal sealed class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _items = [];

    public override int Count => _items.Count;
    public override object SyncRoot => ((ICollection)_items).SyncRoot;

    public override int Add(object value)
    {
        _items.Add((DbParameter)value);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var value in values)
            Add(value!);
    }

    public override void Clear() => _items.Clear();

    public override bool Contains(object value) => _items.Contains((DbParameter)value);

    public override bool Contains(string value) => _items.Any(p => p.ParameterName == value);

    public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    public override IEnumerator GetEnumerator() => _items.GetEnumerator();

    protected override DbParameter GetParameter(int index) => _items[index];

    protected override DbParameter GetParameter(string parameterName)
        => _items.First(p => p.ParameterName == parameterName);

    public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);

    public override int IndexOf(string parameterName)
        => _items.FindIndex(p => p.ParameterName == parameterName);

    public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);

    public override void Remove(object value) => _items.Remove((DbParameter)value);

    public override void RemoveAt(int index) => _items.RemoveAt(index);

    public override void RemoveAt(string parameterName)
    {
        var idx = IndexOf(parameterName);
        if (idx >= 0)
            _items.RemoveAt(idx);
    }

    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var idx = IndexOf(parameterName);
        if (idx >= 0)
            _items[idx] = value;
        else
            _items.Add(value);
    }
}
