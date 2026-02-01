using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PlanWriter.API.Common.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.BadRequest,
                ex.Message
            );
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.NotFound,
                ex.Message
            );
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.Forbidden,
                ex.Message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            await WriteProblemAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Ocorreu um erro inesperado."
            );
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string message)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = message,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem)
        );
    }
}