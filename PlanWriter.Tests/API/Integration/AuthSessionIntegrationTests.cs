using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Entities;
using Xunit;

namespace PlanWriter.Tests.API.Integration;

[Collection(AuthApiTestCollection.Name)]
public sealed class AuthSessionIntegrationTests(AuthApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Login_Refresh_Rotation_ShouldInvalidateReusedToken()
    {
        factory.Store.Reset();
        factory.TokenStore.Reset();
        factory.AuditStore.Reset();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "writer@planwriter.com",
            FirstName = "Writer",
            LastName = "User",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        user.ChangePassword(new PasswordHasher<User>().HashPassword(user, "StrongPassword#2026"));
        factory.Store.Seed(user);

        using var client = CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = "writer@planwriter.com",
            Password = "StrongPassword#2026"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensDto>();
        loginTokens.Should().NotBeNull();
        loginTokens!.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenDto
        {
            RefreshToken = loginTokens.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthTokensDto>();
        rotatedTokens.Should().NotBeNull();
        rotatedTokens!.RefreshToken.Should().NotBe(loginTokens.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenDto
        {
            RefreshToken = loginTokens.RefreshToken
        });

        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var chainRevokedResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenDto
        {
            RefreshToken = rotatedTokens.RefreshToken
        });

        chainRevokedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Logout_ShouldRevokeCurrentSessionFamily()
    {
        factory.Store.Reset();
        factory.TokenStore.Reset();
        factory.AuditStore.Reset();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "writer2@planwriter.com",
            FirstName = "Writer",
            LastName = "Two",
            DateOfBirth = new DateTime(1991, 1, 1)
        };
        user.ChangePassword(new PasswordHasher<User>().HashPassword(user, "StrongPassword#2026"));
        factory.Store.Seed(user);

        using var client = CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = "writer2@planwriter.com",
            Password = "StrongPassword#2026"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensDto>();
        loginTokens.Should().NotBeNull();

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenDto
        {
            RefreshToken = loginTokens!.RefreshToken
        });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenDto
        {
            RefreshToken = loginTokens.RefreshToken
        });

        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LogoutAll_ShouldRevokeAllUserSessions()
    {
        factory.Store.Reset();
        factory.TokenStore.Reset();
        factory.AuditStore.Reset();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "writer3@planwriter.com",
            FirstName = "Writer",
            LastName = "Three",
            DateOfBirth = new DateTime(1992, 1, 1)
        };
        user.ChangePassword(new PasswordHasher<User>().HashPassword(user, "StrongPassword#2026"));
        factory.Store.Seed(user);

        using var publicClient = CreateClient();

        var login1 = await publicClient.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = user.Email,
            Password = "StrongPassword#2026"
        });
        login1.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens1 = await login1.Content.ReadFromJsonAsync<AuthTokensDto>();
        tokens1.Should().NotBeNull();

        var login2 = await publicClient.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = user.Email,
            Password = "StrongPassword#2026"
        });
        login2.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens2 = await login2.Content.ReadFromJsonAsync<AuthTokensDto>();
        tokens2.Should().NotBeNull();

        using var authenticatedClient = CreateClient(user.Id);
        var logoutAllResponse = await authenticatedClient.PostAsync("/api/auth/logout-all", null);
        logoutAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refresh1 = await publicClient.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenDto
        {
            RefreshToken = tokens1!.RefreshToken
        });
        refresh1.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var refresh2 = await publicClient.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenDto
        {
            RefreshToken = tokens2!.RefreshToken
        });
        refresh2.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangePassword_ShouldRevokeAllRefreshSessions()
    {
        factory.Store.Reset();
        factory.TokenStore.Reset();
        factory.AuditStore.Reset();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "writer4@planwriter.com",
            FirstName = "Writer",
            LastName = "Four",
            DateOfBirth = new DateTime(1993, 1, 1)
        };
        user.ChangePassword(new PasswordHasher<User>().HashPassword(user, "StrongPassword#2026"));
        factory.Store.Seed(user);

        using var publicClient = CreateClient();

        var loginResponse = await publicClient.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = user.Email,
            Password = "StrongPassword#2026"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensDto>();
        tokens.Should().NotBeNull();

        using var authenticatedClient = CreateClient(user.Id);
        var changePasswordResponse = await authenticatedClient.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordDto { NewPassword = "StrongPassword#2027" });
        changePasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshAfterPasswordChange = await publicClient.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenDto
        {
            RefreshToken = tokens!.RefreshToken
        });

        refreshAfterPasswordChange.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_ShouldPersistAuditWithCorrelationId()
    {
        factory.Store.Reset();
        factory.TokenStore.Reset();
        factory.AuditStore.Reset();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "writer-audit@planwriter.com",
            FirstName = "Writer",
            LastName = "Audit",
            DateOfBirth = new DateTime(1994, 1, 1)
        };
        user.ChangePassword(new PasswordHasher<User>().HashPassword(user, "StrongPassword#2026"));
        factory.Store.Seed(user);

        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-auth-001");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PlanWriterTests/1.0");

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = user.Email,
            Password = "StrongPassword#2026"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await factory.AuditStore.GetAsync(
            null,
            null,
            null,
            "Login",
            "Success",
            10,
            CancellationToken.None);

        logs.Should().ContainSingle();
        logs[0].CorrelationId.Should().Be("corr-auth-001");
        logs[0].TraceId.Should().NotBeNullOrWhiteSpace();
        logs[0].UserAgent.Should().Contain("PlanWriterTests/1.0");
    }

    private HttpClient CreateClient(Guid? authenticatedUserId = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        if (authenticatedUserId.HasValue)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, authenticatedUserId.Value.ToString());
        }

        return client;
    }
}
