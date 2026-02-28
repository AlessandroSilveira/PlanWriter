using Xunit;

namespace PlanWriter.Tests.API.Integration;

[CollectionDefinition(Name)]
public sealed class HealthApiTestCollection : ICollectionFixture<HealthApiWebApplicationFactory>
{
    public const string Name = "Health API Integration";
}
