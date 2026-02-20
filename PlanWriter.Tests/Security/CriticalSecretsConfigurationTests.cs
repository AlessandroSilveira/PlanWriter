using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PlanWriter.API.Security;
using Xunit;

namespace PlanWriter.Tests.Security;

public class CriticalSecretsConfigurationTests
{
    [Fact]
    public void ValidateForStartup_ShouldSkipValidation_InTestingEnvironment()
    {
        var configuration = BuildConfiguration(
            "Server=localhost,1433;Database=PlanWriterDb;User Id=sa;Password=CHANGE_ME_SQL_PASSWORD;TrustServerCertificate=True;",
            "CHANGE_ME_JWT_KEY_WITH_AT_LEAST_32_CHARS");
        var environment = new FakeHostEnvironment("Testing");

        var act = () => CriticalSecretsConfiguration.ValidateForStartup(configuration, environment);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateForStartup_ShouldThrow_WhenConnectionStringIsMissing()
    {
        var configuration = BuildConfiguration(
            string.Empty,
            "test-jwt-key-with-at-least-32-characters");
        var environment = new FakeHostEnvironment("Staging");

        var act = () => CriticalSecretsConfiguration.ValidateForStartup(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:DefaultConnection*");
    }

    [Fact]
    public void ValidateForStartup_ShouldThrow_WhenConnectionStringUsesPlaceholder()
    {
        var configuration = BuildConfiguration(
            "Server=localhost,1433;Database=PlanWriterDb;User Id=sa;Password=CHANGE_ME_SQL_PASSWORD;TrustServerCertificate=True;",
            "test-jwt-key-with-at-least-32-characters");
        var environment = new FakeHostEnvironment("Staging");

        var act = () => CriticalSecretsConfiguration.ValidateForStartup(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*insecure placeholder*");
    }

    [Fact]
    public void ValidateForStartup_ShouldThrow_WhenJwtKeyUsesPlaceholder()
    {
        var configuration = BuildConfiguration(
            "Server=localhost,1433;Database=PlanWriterDb;User Id=sa;Password=StrongPassword#123;TrustServerCertificate=True;",
            "CHANGE_ME_JWT_KEY_WITH_AT_LEAST_32_CHARS");
        var environment = new FakeHostEnvironment("Staging");

        var act = () => CriticalSecretsConfiguration.ValidateForStartup(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key*insecure placeholder*");
    }

    [Fact]
    public void ValidateForStartup_ShouldThrow_WhenJwtKeyIsWeak()
    {
        var configuration = BuildConfiguration(
            "Server=localhost,1433;Database=PlanWriterDb;User Id=sa;Password=StrongPassword#123;TrustServerCertificate=True;",
            "short-key");
        var environment = new FakeHostEnvironment("Staging");

        var act = () => CriticalSecretsConfiguration.ValidateForStartup(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is weak*");
    }

    [Fact]
    public void ValidateForStartup_ShouldNotThrow_WhenCriticalSecretsAreConfigured()
    {
        var configuration = BuildConfiguration(
            "Server=localhost,1433;Database=PlanWriterDb;User Id=sa;Password=VeryStrongSqlPassword#2026;TrustServerCertificate=True;",
            "this-is-a-very-strong-jwt-key-2026-abcdef");
        var environment = new FakeHostEnvironment("Staging");

        var act = () => CriticalSecretsConfiguration.ValidateForStartup(configuration, environment);

        act.Should().NotThrow();
    }

    private static IConfiguration BuildConfiguration(string connectionString, string jwtKey)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:Key"] = jwtKey
            })
            .Build();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "PlanWriter.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
