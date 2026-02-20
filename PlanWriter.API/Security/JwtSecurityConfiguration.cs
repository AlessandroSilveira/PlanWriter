using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using PlanWriter.Domain.Configurations;

namespace PlanWriter.API.Security;

public static class JwtSecurityConfiguration
{
    private static readonly string[] WeakKeys =
    [
        "SUA_CHAVE_SECRETA_GRANDE_E_UNICA_AQUI",
        "CHANGE_ME",
        "YOUR_SECRET_KEY",
        "JWT_SECRET"
    ];

    public static void ValidateForStartup(JwtOptions options, bool isProduction)
    {
        if (options is null)
        {
            throw new InvalidOperationException("JWT configuration is missing.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("Jwt:Audience must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            throw new InvalidOperationException("Jwt:Key must be configured.");
        }

        if (options.ClockSkewSeconds < 0 || options.ClockSkewSeconds > 300)
        {
            throw new InvalidOperationException("Jwt:ClockSkewSeconds must be between 0 and 300.");
        }

        if (isProduction && IsWeakKey(options.Key))
        {
            throw new InvalidOperationException(
                "Jwt:Key is insecure for production. Use a strong key with at least 32 characters.");
        }

        if (string.IsNullOrWhiteSpace(options.CurrentKid))
        {
            throw new InvalidOperationException("Jwt:CurrentKid must be configured.");
        }

        var kids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            options.CurrentKid.Trim()
        };

        foreach (var previous in options.PreviousKeys ?? [])
        {
            if (string.IsNullOrWhiteSpace(previous.Kid))
            {
                throw new InvalidOperationException("Every Jwt:PreviousKeys entry must define Kid.");
            }

            if (string.IsNullOrWhiteSpace(previous.Key))
            {
                throw new InvalidOperationException($"Jwt:PreviousKeys:{previous.Kid}:Key must be configured.");
            }

            if (!kids.Add(previous.Kid.Trim()))
            {
                throw new InvalidOperationException($"Duplicate JWT key id detected: {previous.Kid}.");
            }
        }
    }

    public static IReadOnlyDictionary<string, SymmetricSecurityKey> BuildSigningKeys(JwtOptions options)
    {
        var keys = new Dictionary<string, SymmetricSecurityKey>(StringComparer.OrdinalIgnoreCase);

        AddKey(keys, options.CurrentKid, options.Key);

        foreach (var previous in options.PreviousKeys ?? [])
        {
            AddKey(keys, previous.Kid, previous.Key);
        }

        return keys;
    }

    public static bool IsWeakKey(string key)
    {
        var normalized = (key ?? string.Empty).Trim();
        if (normalized.Length < 32)
        {
            return true;
        }

        return WeakKeys.Any(k => string.Equals(k, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddKey(IDictionary<string, SymmetricSecurityKey> keys, string kid, string key)
    {
        var normalizedKid = kid.Trim();
        var normalizedKey = key.Trim();
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(normalizedKey))
        {
            KeyId = normalizedKid
        };

        keys[normalizedKid] = securityKey;
    }
}
