using Xunit;

namespace PlanWriter.Tests.API.Integration;

[CollectionDefinition(Name)]
public sealed class AuthApiTestCollection : ICollectionFixture<AuthApiWebApplicationFactory>
{
    public const string Name = "Auth API Integration";
}
