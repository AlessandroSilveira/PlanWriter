using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.API.Security;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Security;

public class AdminBootstrapperTests
{
    [Fact]
    public void ValidateConfiguration_ShouldNotThrow_WhenDisabled()
    {
        var options = new AuthBootstrapOptions
        {
            Enabled = false
        };

        var act = () => AdminBootstrapper.ValidateConfiguration(options, isProduction: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_ShouldThrow_WhenEnabledAndEmailIsMissing()
    {
        var options = new AuthBootstrapOptions
        {
            Enabled = true,
            AdminEmail = "",
            AdminPassword = "StrongPassword#123"
        };

        var act = () => AdminBootstrapper.ValidateConfiguration(options, isProduction: false);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AdminEmail*");
    }

    [Fact]
    public void ValidateConfiguration_ShouldThrow_WhenEnabledAndPasswordIsWeak()
    {
        var options = new AuthBootstrapOptions
        {
            Enabled = true,
            AdminEmail = "admin@example.com",
            AdminPassword = "admin123"
        };

        var act = () => AdminBootstrapper.ValidateConfiguration(options, isProduction: false);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AdminPassword*");
    }

    [Fact]
    public async Task EnsureBootstrapAdminAsync_ShouldCreateAdminWithMustChangePassword_WhenEnabledAndUserDoesNotExist()
    {
        var options = new AuthBootstrapOptions
        {
            Enabled = true,
            AdminEmail = "Admin@Example.com",
            AdminPassword = "StrongPassword#123",
            AdminFirstName = "Security",
            AdminLastName = "Admin"
        };

        var userReadRepositoryMock = new Mock<IUserReadRepository>(MockBehavior.Strict);
        var userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        var passwordHasherMock = new Mock<IPasswordHasher<User>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger>(MockBehavior.Loose);

        User? createdUser = null;

        userReadRepositoryMock
            .Setup(r => r.GetByEmailAsync("admin@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        passwordHasherMock
            .Setup(h => h.HashPassword(It.IsAny<User>(), options.AdminPassword))
            .Returns("hashed-password");

        userRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => createdUser = user)
            .Returns(Task.CompletedTask);

        await AdminBootstrapper.EnsureBootstrapAdminAsync(
            options,
            userReadRepositoryMock.Object,
            userRepositoryMock.Object,
            passwordHasherMock.Object,
            loggerMock.Object,
            CancellationToken.None);

        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be("admin@example.com");
        createdUser.FirstName.Should().Be("Security");
        createdUser.LastName.Should().Be("Admin");
        createdUser.IsAdmin.Should().BeTrue();
        createdUser.MustChangePassword.Should().BeTrue();
        createdUser.PasswordHash.Should().Be("hashed-password");
    }

    [Fact]
    public async Task EnsureBootstrapAdminAsync_ShouldSkipCreation_WhenAdminAlreadyExists()
    {
        var options = new AuthBootstrapOptions
        {
            Enabled = true,
            AdminEmail = "admin@example.com",
            AdminPassword = "StrongPassword#123"
        };

        var userReadRepositoryMock = new Mock<IUserReadRepository>(MockBehavior.Strict);
        var userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        var passwordHasherMock = new Mock<IPasswordHasher<User>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger>(MockBehavior.Loose);

        var existingAdmin = new User { Email = "admin@example.com" };
        existingAdmin.MakeAdmin();

        userReadRepositoryMock
            .Setup(r => r.GetByEmailAsync("admin@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAdmin);

        await AdminBootstrapper.EnsureBootstrapAdminAsync(
            options,
            userReadRepositoryMock.Object,
            userRepositoryMock.Object,
            passwordHasherMock.Object,
            loggerMock.Object,
            CancellationToken.None);

        userRepositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnsureBootstrapAdminAsync_ShouldThrow_WhenEmailAlreadyExistsAsRegularUser()
    {
        var options = new AuthBootstrapOptions
        {
            Enabled = true,
            AdminEmail = "admin@example.com",
            AdminPassword = "StrongPassword#123"
        };

        var userReadRepositoryMock = new Mock<IUserReadRepository>(MockBehavior.Strict);
        var userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        var passwordHasherMock = new Mock<IPasswordHasher<User>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger>(MockBehavior.Loose);

        var existingUser = new User { Email = "admin@example.com" };
        existingUser.MakeRegularUser();

        userReadRepositoryMock
            .Setup(r => r.GetByEmailAsync("admin@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var act = async () => await AdminBootstrapper.EnsureBootstrapAdminAsync(
            options,
            userReadRepositoryMock.Object,
            userRepositoryMock.Object,
            passwordHasherMock.Object,
            loggerMock.Object,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exists and is not an admin*");
    }

    [Fact]
    public async Task EnsureBootstrapAdminAsync_ShouldReturnImmediately_WhenDisabled()
    {
        var options = new AuthBootstrapOptions
        {
            Enabled = false
        };

        var userReadRepositoryMock = new Mock<IUserReadRepository>(MockBehavior.Strict);
        var userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        var passwordHasherMock = new Mock<IPasswordHasher<User>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger>(MockBehavior.Loose);

        await AdminBootstrapper.EnsureBootstrapAdminAsync(
            options,
            userReadRepositoryMock.Object,
            userRepositoryMock.Object,
            passwordHasherMock.Object,
            loggerMock.Object,
            CancellationToken.None);

        userReadRepositoryMock.Verify(
            r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
