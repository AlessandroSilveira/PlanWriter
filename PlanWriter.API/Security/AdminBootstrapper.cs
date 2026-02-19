using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.API.Security;

public static class AdminBootstrapper
{
    public static void ValidateConfiguration(AuthBootstrapOptions options, bool isProduction)
    {
        if (!options.Enabled)
        {
            return;
        }

        var email = NormalizeEmail(options.AdminEmail);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "AuthBootstrap.Enabled=true, but AuthBootstrap.AdminEmail was not configured.");
        }

        if (!IsValidEmail(email))
        {
            throw new InvalidOperationException(
                "AuthBootstrap.AdminEmail has invalid format.");
        }

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            throw new InvalidOperationException(
                "AuthBootstrap.Enabled=true, but AuthBootstrap.AdminPassword was not configured.");
        }

        if (!IsStrongPassword(options.AdminPassword))
        {
            throw new InvalidOperationException(
                "AuthBootstrap.AdminPassword must have at least 12 characters and include uppercase, lowercase, number and symbol.");
        }

        if (isProduction && IsKnownInsecurePassword(options.AdminPassword))
        {
            throw new InvalidOperationException(
                "In production, AuthBootstrap.AdminPassword must not use known insecure values.");
        }
    }

    public static async Task EnsureBootstrapAdminAsync(
        AuthBootstrapOptions options,
        IUserReadRepository userReadRepository,
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Auth bootstrap disabled. Skipping admin bootstrap.");
            return;
        }

        var email = NormalizeEmail(options.AdminEmail);
        var existingUser = await userReadRepository.GetByEmailAsync(email, ct);
        if (existingUser is not null)
        {
            if (!existingUser.IsAdmin)
            {
                throw new InvalidOperationException(
                    $"Auth bootstrap email '{email}' already exists and is not an admin.");
            }

            logger.LogInformation("Bootstrap admin already exists. Skipping creation.");
            return;
        }

        var firstName = string.IsNullOrWhiteSpace(options.AdminFirstName)
            ? "Admin"
            : options.AdminFirstName.Trim();
        var lastName = string.IsNullOrWhiteSpace(options.AdminLastName)
            ? "System"
            : options.AdminLastName.Trim();

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            IsProfilePublic = false,
            DisplayName = firstName,
            DateOfBirth = new DateTime(2000, 1, 1)
        };

        // Keep MustChangePassword=true for first login by setting admin flag after password hash.
        user.ChangePassword(passwordHasher.HashPassword(user, options.AdminPassword));
        user.MakeAdmin();

        await userRepository.CreateAsync(user, ct);

        logger.LogWarning(
            "Bootstrap admin created for {Email}. Password change is required on first login.",
            email);
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return MailAddress.TryCreate(email, out _);
    }

    private static bool IsKnownInsecurePassword(string password)
    {
        var normalized = (password ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "admin" or "password" or "123456" or "admin123";
    }

    private static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            return false;
        }

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        var hasSymbol = false;

        foreach (var ch in password)
        {
            if (char.IsUpper(ch))
            {
                hasUpper = true;
                continue;
            }

            if (char.IsLower(ch))
            {
                hasLower = true;
                continue;
            }

            if (char.IsDigit(ch))
            {
                hasDigit = true;
                continue;
            }

            if (char.IsPunctuation(ch) || char.IsSymbol(ch))
            {
                hasSymbol = true;
            }
        }

        return hasUpper && hasLower && hasDigit && hasSymbol;
    }
}
