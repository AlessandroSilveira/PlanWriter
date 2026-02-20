using FluentAssertions;
using PlanWriter.API.Security;
using PlanWriter.Domain.Configurations;
using Xunit;

namespace PlanWriter.Tests.Security;

public class JwtSecurityConfigurationTests
{
    [Fact]
    public void ValidateForStartup_ShouldThrow_WhenKeyIsWeakInProduction()
    {
        var options = BuildValidOptions();
        options.Key = "SUA_CHAVE_SECRETA_GRANDE_E_UNICA_AQUI";

        var act = () => JwtSecurityConfiguration.ValidateForStartup(options, isProduction: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is insecure*");
    }

    [Fact]
    public void ValidateForStartup_ShouldNotThrow_WhenKeyIsWeakOutsideProduction()
    {
        var options = BuildValidOptions();
        options.Key = "weak-key";

        var act = () => JwtSecurityConfiguration.ValidateForStartup(options, isProduction: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateForStartup_ShouldThrow_WhenClockSkewIsOutOfRange()
    {
        var options = BuildValidOptions();
        options.ClockSkewSeconds = 999;

        var act = () => JwtSecurityConfiguration.ValidateForStartup(options, isProduction: false);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ClockSkewSeconds*");
    }

    [Fact]
    public void BuildSigningKeys_ShouldIncludeCurrentAndPreviousByKid()
    {
        var options = BuildValidOptions();
        options.CurrentKid = "k-current";
        options.PreviousKeys =
        [
            new JwtPreviousKeyOptions
            {
                Kid = "k-old",
                Key = "this-is-a-previous-test-key-1234567890"
            }
        ];

        var keys = JwtSecurityConfiguration.BuildSigningKeys(options);

        keys.Should().ContainKey("k-current");
        keys.Should().ContainKey("k-old");
        keys["k-current"].KeyId.Should().Be("k-current");
        keys["k-old"].KeyId.Should().Be("k-old");
    }

    private static JwtOptions BuildValidOptions()
    {
        return new JwtOptions
        {
            Key = "this-is-a-very-long-test-key-1234567890",
            Issuer = "planwriter-iss",
            Audience = "planwriter-aud",
            CurrentKid = "v1",
            ClockSkewSeconds = 30,
            PreviousKeys = []
        };
    }
}
