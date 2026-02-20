using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
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
        var sut = new JwtTokenGenerator(
            Options.Create(new JwtOptions
            {
                Key = "this-is-a-very-long-test-key-1234567890",
                Audience = "planwriter-aud",
                Issuer = "planwriter-iss",
                CurrentKid = "k1"
            }),
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

        var token = sut.Generate(user, adminMfaVerified: true);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Claims.Select(c => c.Value).Should().Contain(user.Id.ToString());
        parsed.Claims.Select(c => c.Value).Should().Contain("user@test.com");
        parsed.Claims.Select(c => c.Value).Should().Contain("Alice");
        parsed.Claims.First(c => c.Type == "isAdmin").Value.Should().Be("true");
        parsed.Claims.First(c => c.Type == "adminMfaVerified").Value.Should().Be("true");
        parsed.Claims.First(c => c.Type == "mustChangePassword").Value.Should().Be("false");
        parsed.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        parsed.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
        parsed.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Nbf);
        parsed.Header.Kid.Should().Be("k1");
        parsed.Issuer.Should().Be("planwriter-iss");
        parsed.Audiences.Should().Contain("planwriter-aud");
    }

    [Fact]
    public void Generate_ShouldThrow_WhenKeyIsMissing()
    {
        var sut = new JwtTokenGenerator(
            Options.Create(new JwtOptions
            {
                Key = string.Empty,
                Audience = "planwriter-aud",
                Issuer = "planwriter-iss",
                CurrentKid = "k1"
            }),
            Options.Create(new AuthTokenOptions
            {
                AccessTokenMinutes = 15,
                RefreshTokenDays = 7
            }));

        var act = () => sut.Generate(new User { Id = Guid.NewGuid(), Email = "user@test.com", FirstName = "Alice" }, adminMfaVerified: false);

        act.Should().Throw<ArgumentException>();
    }
}
