using System;
using System.Security.Cryptography;

namespace PlanWriter.Application.Security;

public static class RefreshTokenSecurity
{
    public static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);

        var base64 = Convert.ToBase64String(bytes);
        return base64
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string HashToken(string token)
    {
        var normalized = (token ?? string.Empty).Trim();
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
