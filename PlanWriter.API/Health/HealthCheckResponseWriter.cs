using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PlanWriter.API.Health;

public static class HealthCheckResponseWriter
{
    public static async Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            timestampUtc = DateTime.UtcNow,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                    error = entry.Value.Exception?.Message,
                    data = entry.Value.Data
                })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
