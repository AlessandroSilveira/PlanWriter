using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Requests;
using Xunit;

namespace PlanWriter.Tests.API.Integration;

[Collection(ProfileApiTestCollection.Name)]
public sealed class ProfileControllerIntegrationTests(ProfileApiWebApplicationFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task UpdateMine_ThenGetMine_ShouldPersistProfileData()
    {
        var userId = Guid.NewGuid();
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();

        SeedProfileState(
            users:
            [
                new User
                {
                    Id = userId,
                    Email = "writer@planwriter.com",
                    DisplayName = "Nome antigo",
                    DateOfBirth = new DateTime(1998, 1, 1),
                    IsProfilePublic = false
                }
            ],
            projects:
            [
                new Project
                {
                    Id = projectA,
                    UserId = userId,
                    Title = "Projeto A",
                    CurrentWordCount = 1100,
                    IsPublic = false,
                    GoalAmount = 10000,
                    WordCountGoal = 10000,
                    StartDate = DateTime.UtcNow.Date
                },
                new Project
                {
                    Id = projectB,
                    UserId = userId,
                    Title = "Projeto B",
                    CurrentWordCount = 300,
                    IsPublic = false,
                    GoalAmount = 5000,
                    WordCountGoal = 5000,
                    StartDate = DateTime.UtcNow.Date
                }
            ]);

        using var client = CreateClient(userId);

        var updateRequest = new UpdateMyProfileRequest(
            DisplayName: "  Nome Novo  ",
            Bio: "  Escrevendo todos os dias  ",
            AvatarUrl: " https://img.local/avatar.png ",
            IsProfilePublic: true,
            Slug: "Perfil Único",
            PublicProjectIds: [projectA]
        );

        var updateResponse = await client.PutAsJsonAsync("/api/profile/me", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedProfile = await updateResponse.Content.ReadFromJsonAsync<MyProfileDto>(JsonOptions);
        updatedProfile.Should().NotBeNull();
        updatedProfile!.DisplayName.Should().Be("Nome Novo");
        updatedProfile.Bio.Should().Be("Escrevendo todos os dias");
        updatedProfile.AvatarUrl.Should().Be("https://img.local/avatar.png");
        updatedProfile.IsProfilePublic.Should().BeTrue();
        updatedProfile.Slug.Should().Be("perfil-unico");
        updatedProfile.PublicProjectIds.Should().BeEquivalentTo([projectA]);

        var getResponse = await client.GetAsync("/api/profile/me");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getProfile = await getResponse.Content.ReadFromJsonAsync<MyProfileDto>(JsonOptions);
        getProfile.Should().NotBeNull();
        getProfile!.DisplayName.Should().Be("Nome Novo");
        getProfile.Bio.Should().Be("Escrevendo todos os dias");
        getProfile.AvatarUrl.Should().Be("https://img.local/avatar.png");
        getProfile.IsProfilePublic.Should().BeTrue();
        getProfile.Slug.Should().Be("perfil-unico");
        getProfile.PublicProjectIds.Should().BeEquivalentTo([projectA]);
    }

    [Fact]
    public async Task GetMine_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        SeedProfileState();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/profile/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMine_WithDuplicatedSlug_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        SeedProfileState(
            users:
            [
                new User
                {
                    Id = userId,
                    Email = "owner@planwriter.com",
                    DateOfBirth = new DateTime(1995, 2, 2),
                    DisplayName = "Owner"
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "existing@planwriter.com",
                    DateOfBirth = new DateTime(1994, 3, 3),
                    DisplayName = "Outro",
                    Slug = "ja-em-uso"
                }
            ],
            projects:
            [
                new Project
                {
                    Id = projectId,
                    UserId = userId,
                    Title = "Projeto",
                    IsPublic = false,
                    StartDate = DateTime.UtcNow.Date
                }
            ]);

        using var client = CreateClient(userId);

        var request = new UpdateMyProfileRequest(
            DisplayName: "Owner",
            Bio: "bio",
            AvatarUrl: null,
            IsProfilePublic: true,
            Slug: "Já em uso",
            PublicProjectIds: [projectId]);

        var response = await client.PutAsJsonAsync("/api/profile/me", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Este slug já está em uso.");
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

    private void SeedProfileState(IEnumerable<User>? users = null, IEnumerable<Project>? projects = null)
    {
        var store = factory.Store;
        store.Reset();

        if (users is not null)
        {
            foreach (var user in users)
            {
                store.SeedUser(user);
            }
        }

        if (projects is null)
        {
            return;
        }

        foreach (var project in projects)
        {
            store.SeedProject(project);
        }
    }
}
