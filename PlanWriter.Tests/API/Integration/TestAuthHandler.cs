using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PlanWriter.Tests.API.Integration;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";
    public const string UserIdHeader = "X-Test-UserId";
    public const string IsAdminHeader = "X-Test-IsAdmin";
    public const string MustChangePasswordHeader = "X-Test-MustChangePassword";
    public const string AdminMfaVerifiedHeader = "X-Test-AdminMfaVerified";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var values) || string.IsNullOrWhiteSpace(values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Guid.TryParse(values[0], out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid test user id."));
        }

        var isAdmin = Request.Headers.TryGetValue(IsAdminHeader, out var isAdminValues) &&
                      bool.TryParse(isAdminValues[0], out var parsedIsAdmin) &&
                      parsedIsAdmin;
        var mustChangePassword = Request.Headers.TryGetValue(MustChangePasswordHeader, out var mustChangeValues) &&
                                 bool.TryParse(mustChangeValues[0], out var parsedMustChange) &&
                                 parsedMustChange;
        var adminMfaVerified = Request.Headers.TryGetValue(AdminMfaVerifiedHeader, out var adminMfaValues) &&
                               bool.TryParse(adminMfaValues[0], out var parsedAdminMfa) &&
                               parsedAdminMfa;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("isAdmin", isAdmin.ToString().ToLowerInvariant()),
            new Claim("mustChangePassword", mustChangePassword.ToString().ToLowerInvariant()),
            new Claim("adminMfaVerified", adminMfaVerified.ToString().ToLowerInvariant())
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
