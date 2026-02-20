using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Entities;
using Xunit;

namespace PlanWriter.Tests.API.Integration;

[Collection(AuthApiTestCollection.Name)]
public sealed class AuthControllerIntegrationTests(AuthApiWebApplicationFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        factory.Store.Reset();
        factory.AuditStore.Reset();
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", BuildRegisterDto("123456"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Errors.SelectMany(e => e.Value)
            .Should().Contain(m => m.Contains("pelo menos 12 caracteres", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_WithCommonPassword_ShouldReturnBadRequest()
    {
        factory.Store.Reset();
        factory.AuditStore.Reset();
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", BuildRegisterDto("Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Errors.SelectMany(e => e.Value)
            .Should().Contain(m => m.Contains("muito comum", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_WithStrongPassword_ShouldCreateUser()
    {
        factory.Store.Reset();
        factory.AuditStore.Reset();
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", BuildRegisterDto("StrongPassword#2026"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.Store.SnapshotUsers().Should().ContainSingle(u => u.Email == "newuser@planwriter.com");
    }

    [Fact]
    public async Task ChangePassword_WithWeakPassword_ShouldReturnBadRequest()
    {
        factory.Store.Reset();
        factory.AuditStore.Reset();

        var userId = Guid.NewGuid();
        factory.Store.Seed(new User
        {
            Id = userId,
            Email = "writer@planwriter.com",
            PasswordHash = "old-hash",
            DateOfBirth = new DateTime(1995, 1, 1)
        });

        using var client = CreateClient(userId);

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordDto { NewPassword = "123456" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Errors.SelectMany(e => e.Value)
            .Should().Contain(m => m.Contains("pelo menos 12 caracteres", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ChangePassword_WithStrongPassword_ShouldUpdatePassword()
    {
        factory.Store.Reset();
        factory.AuditStore.Reset();

        var userId = Guid.NewGuid();
        factory.Store.Seed(new User
        {
            Id = userId,
            Email = "writer@planwriter.com",
            PasswordHash = "old-hash",
            DateOfBirth = new DateTime(1995, 1, 1)
        });

        using var client = CreateClient(userId);

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordDto { NewPassword = "StrongPassword#2026" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var updatedUser = factory.Store.SnapshotUsers().Single(u => u.Id == userId);
        updatedUser.PasswordHash.Should().NotBe("old-hash");
        updatedUser.PasswordHash.Should().NotBe("StrongPassword#2026");
    }

    private static RegisterUserDto BuildRegisterDto(string password)
    {
        return new RegisterUserDto
        {
            FirstName = "Novo",
            LastName = "Usuario",
            Email = "newuser@planwriter.com",
            Password = password,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
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
