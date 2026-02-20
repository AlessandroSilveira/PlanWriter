using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PlanWriter.Domain.Configurations;

namespace PlanWriter.API.Security;

public static class CriticalSecretsConfiguration
{
    private static readonly string[] ForbiddenSecretMarkers =
    [
        "CHANGE_ME_SQL_PASSWORD",
        "CHANGE_ME_JWT_KEY",
        "SUA_CHAVE_SECRETA_GRANDE_E_UNICA_AQUI",
        "Str0ng!Senha2024",
        "YourStrong!Passw0rd",
        "YOUR_SECRET_KEY",
        "JWT_SECRET",
        "__SET_IN_ENV__"
    ];

    public static void ValidateForStartup(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured with a per-environment secret.");
        }

        if (!connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must include a password secret.");
        }

        if (ContainsForbiddenMarker(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection contains an insecure placeholder/default value. Configure a real secret per environment.");
        }

        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.Key))
        {
            throw new InvalidOperationException("Jwt:Key must be configured with a per-environment secret.");
        }

        if (ContainsForbiddenMarker(jwtOptions.Key))
        {
            throw new InvalidOperationException(
                "Jwt:Key contains an insecure placeholder/default value. Configure a real secret per environment.");
        }

        if (JwtSecurityConfiguration.IsWeakKey(jwtOptions.Key))
        {
            throw new InvalidOperationException(
                "Jwt:Key is weak. Use at least 32 characters with enough entropy.");
        }
    }

    private static bool ContainsForbiddenMarker(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        foreach (var marker in ForbiddenSecretMarkers)
        {
            if (normalized.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
