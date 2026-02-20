using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.Tests.API.Integration;

public sealed class AuthApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserReadRepository>();
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IUserRegistrationReadRepository>();
            services.RemoveAll<IUserRegistrationRepository>();
            services.RemoveAll<IUserPasswordRepository>();
            services.RemoveAll<IUserAuthReadRepository>();
            services.RemoveAll<IAuthAuditReadRepository>();
            services.RemoveAll<IAuthAuditRepository>();
            services.RemoveAll<IRefreshTokenRepository>();
            services.RemoveAll<IJwtTokenGenerator>();

            services.AddSingleton<InMemoryAuthRepository>();
            services.AddSingleton<IUserReadRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserRegistrationReadRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserRegistrationRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserPasswordRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserAuthReadRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());

            services.AddSingleton<InMemoryRefreshTokenRepository>();
            services.AddSingleton<IRefreshTokenRepository>(sp => sp.GetRequiredService<InMemoryRefreshTokenRepository>());

            services.AddSingleton<InMemoryAuthAuditRepository>();
            services.AddSingleton<IAuthAuditRepository>(sp => sp.GetRequiredService<InMemoryAuthAuditRepository>());
            services.AddSingleton<IAuthAuditReadRepository>(sp => sp.GetRequiredService<InMemoryAuthAuditRepository>());
            services.AddSingleton<IJwtTokenGenerator, FakeJwtTokenGenerator>();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public InMemoryAuthRepository Store => Services.GetRequiredService<InMemoryAuthRepository>();
    public InMemoryRefreshTokenRepository TokenStore => Services.GetRequiredService<InMemoryRefreshTokenRepository>();
    public InMemoryAuthAuditRepository AuditStore => Services.GetRequiredService<InMemoryAuthAuditRepository>();
}
