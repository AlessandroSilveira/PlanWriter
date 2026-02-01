using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlanWriter.Application.Common.Behaviors;


namespace PlanWriter.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                Assembly.GetExecutingAssembly()
            )
        );

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(LoggingBehavior<,>)
        );

        return services;
    }
}