using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.API.Health;

public sealed class SqlServerConnectionHealthCheck(IDbConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = connectionFactory.CreateConnection();

            if (connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";

            var probeResult = command.ExecuteScalar();
            if (probeResult is null || Convert.ToInt32(probeResult) != 1)
            {
                return HealthCheckResult.Unhealthy(
                    description: "Database probe returned an unexpected result.",
                    data: new Dictionary<string, object>
                    {
                        ["probeResult"] = probeResult ?? "null"
                    });
            }

            return HealthCheckResult.Healthy(
                description: "Database connection is healthy.",
                data: new Dictionary<string, object>
                {
                    ["database"] = connection.Database,
                    ["dataSource"] = connection is DbConnection concreteConnection
                        ? concreteConnection.DataSource
                        : string.Empty
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                description: "Database connectivity check failed.",
                exception: ex);
        }
    }
}
