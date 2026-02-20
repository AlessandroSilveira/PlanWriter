using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Auth;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Auth;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void Generate_ShouldCreateTokenWithExpectedClaims()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-very-long-test-key-1234567890",
                ["Jwt:Audience"] = "planwriter-aud",
                ["Jwt:Issuer"] = "planwriter-iss"
            })
            .Build();

        var sut = new JwtTokenGenerator(
            config,
            Options.Create(new AuthTokenOptions
            {
                AccessTokenMinutes = 15,
                RefreshTokenDays = 7
            }));
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            FirstName = "Alice"
        };
        user.MakeAdmin();
        user.ChangePassword("hash");

        var token = sut.Generate(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Claims.Select(c => c.Value).Should().Contain(user.Id.ToString());
        parsed.Claims.Select(c => c.Value).Should().Contain("user@test.com");
        parsed.Claims.Select(c => c.Value).Should().Contain("Alice");
        parsed.Claims.First(c => c.Type == "isAdmin").Value.Should().Be("true");
        parsed.Claims.First(c => c.Type == "mustChangePassword").Value.Should().Be("false");
        parsed.Issuer.Should().Be("planwriter-iss");
        parsed.Audiences.Should().Contain("planwriter-aud");
    }

    [Fact]
    public void Generate_ShouldThrow_WhenKeyIsMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var sut = new JwtTokenGenerator(
            config,
            Options.Create(new AuthTokenOptions
            {
                AccessTokenMinutes = 15,
                RefreshTokenDays = 7
            }));

        var act = () => sut.Generate(new User { Id = Guid.NewGuid(), Email = "user@test.com", FirstName = "Alice" });

        act.Should().Throw<ArgumentNullException>();
    }
}
