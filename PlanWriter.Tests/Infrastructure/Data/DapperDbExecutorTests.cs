using System.Data;
using FluentAssertions;
using Moq;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Tests.Infrastructure.Fakes;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Data;

public class DapperDbExecutorTests
{
    [Fact]
    public async Task QueryAsync_ShouldOpenConnection_WhenClosed()
    {
        var conn = new FakeDbConnection();
        var table = new DataTable();
        table.Columns.Add("Value", typeof(int));
        table.Rows.Add(42);
        conn.QueryResults.Enqueue(table);

        var factory = new Mock<IDbConnectionFactory>();
        factory.Setup(x => x.CreateConnection()).Returns(conn);

        var sut = new DapperDbExecutor(factory.Object);

        var result = await sut.QueryAsync<int>("SELECT 42");

        result.Should().ContainSingle().Which.Should().Be(42);
        conn.OpenCalls.Should().Be(1);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ShouldReadFirstRow()
    {
        var conn = new FakeDbConnection();
        var table = new DataTable();
        table.Columns.Add("Value", typeof(string));
        table.Rows.Add("ok");
        conn.QueryResults.Enqueue(table);

        var factory = new Mock<IDbConnectionFactory>();
        factory.Setup(x => x.CreateConnection()).Returns(conn);

        var sut = new DapperDbExecutor(factory.Object);

        var result = await sut.QueryFirstOrDefaultAsync<string>("SELECT 'ok'");

        result.Should().Be("ok");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAffectedRows()
    {
        var conn = new FakeDbConnection();
        conn.NonQueryResults.Enqueue(3);

        var factory = new Mock<IDbConnectionFactory>();
        factory.Setup(x => x.CreateConnection()).Returns(conn);

        var sut = new DapperDbExecutor(factory.Object);

        var affected = await sut.ExecuteAsync("UPDATE X");

        affected.Should().Be(3);
    }

    [Fact]
    public async Task QueryAsync_ShouldNotOpen_WhenConnectionAlreadyOpen()
    {
        var conn = new FakeDbConnection();
        conn.ForceOpenState();

        var table = new DataTable();
        table.Columns.Add("Value", typeof(int));
        table.Rows.Add(10);
        conn.QueryResults.Enqueue(table);

        var factory = new Mock<IDbConnectionFactory>();
        factory.Setup(x => x.CreateConnection()).Returns(conn);

        var sut = new DapperDbExecutor(factory.Object);

        var result = await sut.QueryAsync<int>("SELECT 10");

        result.Should().ContainSingle().Which.Should().Be(10);
        conn.OpenCalls.Should().Be(0);
    }
}
