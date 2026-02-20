using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Security;
using PlanWriter.Domain.Entities;
using Xunit;

namespace PlanWriter.Tests.API.Integration;

[Collection(AuthApiTestCollection.Name)]
public sealed class AdminMfaIntegrationTests(AuthApiWebApplicationFactory factory)
{
    [Fact]
    public async Task AdminArea_ShouldReturnForbidden_WhenAdminMfaClaimIsFalse()
    {
        factory.Store.Reset();
        factory.AuditStore.Reset();

        using var client = CreateClient(
            Guid.NewGuid(),
            isAdmin: true,
            adminMfaVerified: false);

        var response = await client.GetAsync("/api/admin/security/auth-audits");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminArea_ShouldReturnOk_WhenAdminMfaClaimIsTrue()
    {
        factory.Store.Reset();
        factory.AuditStore.Reset();

        using var client = CreateClient(
            Guid.NewGuid(),
            isAdmin: true,
            adminMfaVerified: true);

        var response = await client.GetAsync("/api/admin/security/auth-audits");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldRequireMfa_WhenAdminMfaIsEnabled()
    {
        factory.Store.Reset();
        factory.AuditStore.Reset();

        var admin = SeedAdminWithMfa("admin-mfa@test.com", "StrongAdmin#2026", out var secret);
        using var client = CreateClient();

        var noMfaResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = admin.Email,
            Password = "StrongAdmin#2026"
        });
        noMfaResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var validCode = AdminMfaSecurity.GenerateCurrentTotpCode(secret, DateTime.UtcNow);
        var withMfaResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = admin.Email,
            Password = "StrongAdmin#2026",
            MfaCode = validCode
        });

        withMfaResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldAcceptBackupCodeOnlyOnce_WhenAdminMfaIsEnabled()
    {
        factory.Store.Reset();
        factory.AuditStore.Reset();

        var admin = SeedAdminWithMfa("admin-backup@test.com", "StrongAdmin#2026", out _);
        var backupCode = "ABCD-EFGH";
        var backupHash = AdminMfaSecurity.HashBackupCode(backupCode);
        await factory.Store.ReplaceBackupCodesAsync(admin.Id, [backupHash], CancellationToken.None);

        using var client = CreateClient();

        var first = await client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = admin.Email,
            Password = "StrongAdmin#2026",
            BackupCode = backupCode
        });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = admin.Email,
            Password = "StrongAdmin#2026",
            BackupCode = backupCode
        });
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private User SeedAdminWithMfa(string email, string password, out string secret)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Admin",
            LastName = "System",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        user.MakeAdmin();

        var hasher = new PasswordHasher<User>();
        user.ChangePassword(hasher.HashPassword(user, password));

        secret = AdminMfaSecurity.GenerateSecretKey();
        user.EnableAdminMfa(secret);
        factory.Store.Seed(user);
        return user;
    }

    private HttpClient CreateClient(
        Guid? authenticatedUserId = null,
        bool isAdmin = false,
        bool adminMfaVerified = false)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        if (!authenticatedUserId.HasValue)
        {
            return client;
        }

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, authenticatedUserId.Value.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.IsAdminHeader, isAdmin.ToString().ToLowerInvariant());
        client.DefaultRequestHeaders.Add(TestAuthHandler.AdminMfaVerifiedHeader, adminMfaVerified.ToString().ToLowerInvariant());
        return client;
    }
}
