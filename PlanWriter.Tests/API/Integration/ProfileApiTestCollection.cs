using Xunit;

namespace PlanWriter.Tests.API.Integration;

[CollectionDefinition(ProfileApiTestCollection.Name, DisableParallelization = true)]
public sealed class ProfileApiTestCollection : ICollectionFixture<ProfileApiWebApplicationFactory>
{
    public const string Name = "Profile API Integration";
}
