using System.Net;
using System.Net.Http.Json;
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

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}
