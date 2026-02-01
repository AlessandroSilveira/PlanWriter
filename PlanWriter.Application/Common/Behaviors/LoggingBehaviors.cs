using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace PlanWriter.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Business rule violation in {Request}",
                typeof(TRequest).Name
            );
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Resource not found in {Request}",
                typeof(TRequest).Name
            );
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled error in {Request}",
                typeof(TRequest).Name
            );
            throw;
        }
    }
}