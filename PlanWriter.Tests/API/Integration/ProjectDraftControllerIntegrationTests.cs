using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using Xunit;

namespace PlanWriter.Tests.API.Integration;

[Collection(ProfileApiTestCollection.Name)]
public sealed class ProjectDraftControllerIntegrationTests(ProfileApiWebApplicationFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetDraft_ShouldReturnNoContent_WhenDraftDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        SeedState(userId, projectId);
        using var client = CreateClient(userId);

        var response = await client.GetAsync($"/api/projects/{projectId}/draft");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SaveDraft_ThenGetDraft_ShouldPersistHtmlContent()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        SeedState(userId, projectId);
        using var client = CreateClient(userId);

        var saveResponse = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/draft",
            new SaveProjectDraftDto { HtmlContent = "<p><strong>Texto rico</strong></p>" });

        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await saveResponse.Content.ReadFromJsonAsync<ProjectDraftDto>(JsonOptions);
        saved.Should().NotBeNull();
        saved!.ProjectId.Should().Be(projectId);
        saved.HtmlContent.Should().Be("<p><strong>Texto rico</strong></p>");

        var getResponse = await client.GetAsync($"/api/projects/{projectId}/draft");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var draft = await getResponse.Content.ReadFromJsonAsync<ProjectDraftDto>(JsonOptions);
        draft.Should().NotBeNull();
        draft!.HtmlContent.Should().Be("<p><strong>Texto rico</strong></p>");
        draft.CreatedAtUtc.Should().NotBe(default);
        draft.UpdatedAtUtc.Should().BeOnOrAfter(draft.CreatedAtUtc);
    }

    [Fact]
    public async Task SaveDraft_ShouldOverwriteExistingDraft_ForSameProject()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        SeedState(userId, projectId);
        using var client = CreateClient(userId);

        var firstResponse = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/draft",
            new SaveProjectDraftDto { HtmlContent = "<p>Primeira versão</p>" });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstDraft = await firstResponse.Content.ReadFromJsonAsync<ProjectDraftDto>(JsonOptions);

        await Task.Delay(20);

        var secondResponse = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/draft",
            new SaveProjectDraftDto { HtmlContent = "<p>Segunda versão</p>" });

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondDraft = await secondResponse.Content.ReadFromJsonAsync<ProjectDraftDto>(JsonOptions);
        secondDraft.Should().NotBeNull();
        secondDraft!.ProjectId.Should().Be(projectId);
        secondDraft.HtmlContent.Should().Be("<p>Segunda versão</p>");
        secondDraft.CreatedAtUtc.Should().Be(firstDraft!.CreatedAtUtc);
        secondDraft.UpdatedAtUtc.Should().BeAfter(firstDraft.UpdatedAtUtc);
    }

    [Fact]
    public async Task SaveDraft_ShouldReturnNotFound_WhenProjectDoesNotBelongToUser()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        SeedState(ownerId, projectId);
        using var client = CreateClient(otherUserId);

        var response = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/draft",
            new SaveProjectDraftDto { HtmlContent = "<p>Outro usuário</p>" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateClient(Guid authenticatedUserId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, authenticatedUserId.ToString());
        return client;
    }

    private void SeedState(Guid userId, Guid projectId)
    {
        factory.Store.Reset();
        factory.ProjectDrafts.Reset();

        factory.Store.SeedUser(new User
        {
            Id = userId,
            Email = "writer@planwriter.com",
            DisplayName = "Writer",
            DateOfBirth = new DateTime(1991, 1, 1)
        });

        factory.Store.SeedProject(new Project
        {
            Id = projectId,
            UserId = userId,
            Title = "Projeto draft",
            StartDate = DateTime.UtcNow.Date
        });
    }
}
