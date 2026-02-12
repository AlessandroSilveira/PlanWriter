using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PlanWriter.Infrastructure.Data;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Data;

public class SqlConnectionFactoryTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenConnectionStringIsMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var act = () => new SqlConnectionFactory(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Connection string 'DefaultConnection' not found.");
    }

    [Fact]
    public void CreateConnection_ShouldReturnSqlConnection_WithConfiguredConnectionString()
    {
        const string connStr = "Server=localhost;Database=PlanWriter;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connStr
            })
            .Build();

        var sut = new SqlConnectionFactory(config);

        var connection = sut.CreateConnection();

        connection.Should().BeOfType<SqlConnection>();
        ((SqlConnection)connection).ConnectionString.Should().Contain("Database=PlanWriter");
    }
}
