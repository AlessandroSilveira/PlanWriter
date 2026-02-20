using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanWriter.API.Middleware;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task Invoke(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["TraceId"] = context.TraceIdentifier,
                   ["CorrelationId"] = correlationId
               }))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString().Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
