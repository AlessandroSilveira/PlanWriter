using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PlanWriter.Application.Security;

public static class AdminMfaSecurity
{
    private const int TotpDigits = 6;
    private const int TimeStepSeconds = 30;
    private const int AllowedDriftWindows = 1;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const string BackupCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string GenerateSecretKey(int bytesLength = 20)
    {
        var bytes = RandomNumberGenerator.GetBytes(Math.Max(16, bytesLength));
        return EncodeBase32(bytes);
    }

    public static string BuildOtpAuthUri(string issuer, string accountEmail, string secret)
    {
        var safeIssuer = string.IsNullOrWhiteSpace(issuer) ? "PlanWriter" : issuer.Trim();
        var safeAccount = string.IsNullOrWhiteSpace(accountEmail) ? "admin" : accountEmail.Trim();

        return string.Create(
            CultureInfo.InvariantCulture,
            $"otpauth://totp/{Uri.EscapeDataString(safeIssuer)}:{Uri.EscapeDataString(safeAccount)}?secret={secret}&issuer={Uri.EscapeDataString(safeIssuer)}&algorithm=SHA1&digits={TotpDigits}&period={TimeStepSeconds}");
    }

    public static bool ValidateTotpCode(string secret, string code, DateTime utcNow)
    {
        var normalizedCode = NormalizeTotpCode(code);
        if (normalizedCode is null)
        {
            return false;
        }

        var keyBytes = DecodeBase32(secret);
        if (keyBytes.Length == 0)
        {
            return false;
        }

        var unixTime = (long)(utcNow - DateTime.UnixEpoch).TotalSeconds;
        var counter = unixTime / TimeStepSeconds;

        for (var drift = -AllowedDriftWindows; drift <= AllowedDriftWindows; drift++)
        {
            var candidate = GenerateTotpCode(keyBytes, counter + drift);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(candidate),
                    Encoding.ASCII.GetBytes(normalizedCode)))
            {
                return true;
            }
        }

        return false;
    }

    public static string GenerateCurrentTotpCode(string secret, DateTime utcNow)
    {
        var keyBytes = DecodeBase32(secret);
        if (keyBytes.Length == 0)
        {
            return string.Empty;
        }

        var unixTime = (long)(utcNow - DateTime.UnixEpoch).TotalSeconds;
        var counter = unixTime / TimeStepSeconds;
        return GenerateTotpCode(keyBytes, counter);
    }

    public static IReadOnlyList<string> GenerateBackupCodes(int count = 8)
    {
        var safeCount = Math.Clamp(count, 5, 20);
        var list = new List<string>(safeCount);

        for (var i = 0; i < safeCount; i++)
        {
            list.Add($"{GenerateBackupCodeGroup(4)}-{GenerateBackupCodeGroup(4)}");
        }

        return list;
    }

    public static string NormalizeBackupCode(string backupCode)
    {
        if (string.IsNullOrWhiteSpace(backupCode))
        {
            return string.Empty;
        }

        var chars = backupCode
            .Trim()
            .ToUpperInvariant()
            .Where(ch => char.IsLetterOrDigit(ch))
            .ToArray();

        return new string(chars);
    }

    public static string HashBackupCode(string backupCode)
    {
        var normalized = NormalizeBackupCode(backupCode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string? NormalizeTotpCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = new string(code.Trim().Where(char.IsDigit).ToArray());
        return normalized.Length == TotpDigits ? normalized : null;
    }

    private static string GenerateBackupCodeGroup(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = BackupCodeAlphabet[bytes[i] % BackupCodeAlphabet.Length];
        }

        return new string(chars);
    }

    private static string GenerateTotpCode(byte[] keyBytes, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var otp = binary % (int)Math.Pow(10, TotpDigits);
        return otp.ToString($"D{TotpDigits}", CultureInfo.InvariantCulture);
    }

    private static string EncodeBase32(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        var outputLength = (int)Math.Ceiling(data.Length / 5d) * 8;
        var output = new char[outputLength];

        var buffer = (int)data[0];
        var next = 1;
        var bitsLeft = 8;
        var index = 0;

        while (index < outputLength)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++] & 0xFF;
                    bitsLeft += 8;
                }
                else
                {
                    var pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }

            var value = (buffer >> (bitsLeft - 5)) & 0x1F;
            bitsLeft -= 5;
            output[index++] = Base32Alphabet[value];
        }

        return new string(output).TrimEnd('=');
    }

    private static byte[] DecodeBase32(string? base32)
    {
        if (string.IsNullOrWhiteSpace(base32))
        {
            return [];
        }

        var normalized = base32.Trim().TrimEnd('=').Replace(" ", string.Empty).ToUpperInvariant();
        var byteCount = normalized.Length * 5 / 8;
        var result = new byte[byteCount];

        var buffer = 0;
        var bitsLeft = 0;
        var index = 0;

        foreach (var ch in normalized)
        {
            var value = Base32Alphabet.IndexOf(ch);
            if (value < 0)
            {
                return [];
            }

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                result[index++] = (byte)((buffer >> (bitsLeft - 8)) & 0xFF);
                bitsLeft -= 8;
            }
        }

        return result;
    }
}
