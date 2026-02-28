using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PlanWriter.Tests.API.Integration;

[Collection(HealthApiTestCollection.Name)]
public sealed class HealthEndpointIntegrationTests(HealthApiWebApplicationFactory factory)
{
    [Fact]
    public async Task GetHealth_ShouldReturnHealthyJsonPayload()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        payload.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        payload.RootElement.GetProperty("checks").GetProperty("sqlserver").GetProperty("status").GetString()
            .Should().Be("Healthy");
    }

    [Fact]
    public async Task GetApiHealth_ShouldReturnHealthyJsonPayload()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        payload.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        payload.RootElement.GetProperty("checks").GetProperty("sqlserver").GetProperty("status").GetString()
            .Should().Be("Healthy");
    }
}
