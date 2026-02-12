using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Tests.API.Integration;

public sealed class ProfileApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserReadRepository>();
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IProjectRepository>();
            services.RemoveAll<IProjectReadRepository>();

            services.AddSingleton<InMemoryProfileStore>();

            services.AddSingleton<InMemoryUserRepository>();
            services.AddSingleton<IUserReadRepository>(sp => sp.GetRequiredService<InMemoryUserRepository>());
            services.AddSingleton<IUserRepository>(sp => sp.GetRequiredService<InMemoryUserRepository>());

            services.AddSingleton<InMemoryProjectRepository>();
            services.AddSingleton<IProjectRepository>(sp => sp.GetRequiredService<InMemoryProjectRepository>());
            services.AddSingleton<IProjectReadRepository>(sp => sp.GetRequiredService<InMemoryProjectRepository>());

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public InMemoryProfileStore Store => Services.GetRequiredService<InMemoryProfileStore>();
}
