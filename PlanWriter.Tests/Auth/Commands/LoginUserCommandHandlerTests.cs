using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlanWriter.Application.Auth.Commands;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Security;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using Xunit;

namespace PlanWriter.Tests.Auth.Commands;

public class LoginUserCommandHandlerTests
{
    private readonly Mock<IUserAuthReadRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IAdminMfaRepository> _adminMfaRepositoryMock = new();
    private readonly Mock<ILogger<LoginUserCommandHandler>> _loggerMock = new();
    private readonly TimeProvider _timeProvider = new FixedTimeProvider(
        new DateTimeOffset(2026, 2, 20, 20, 0, 0, TimeSpan.Zero));
    private readonly IOptions<AuthTokenOptions> _options = Options.Create(new AuthTokenOptions
    {
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    });

    [Fact]
    public async Task Handle_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        var email = "user@test.com";
        var password = "Password123";
        var fakeAccessToken = "fake-jwt-token";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Test",
            PasswordHash = "hashed"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, password))
            .Returns(PasswordVerificationResult.Success);

        _tokenGeneratorMock
            .Setup(t => t.Generate(user, It.IsAny<bool>()))
            .Returns(fakeAccessToken);

        _refreshTokenRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RefreshTokenSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = BuildCommand(email, password);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be(fakeAccessToken);
        result.AccessTokenExpiresInSeconds.Should().Be(900);
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshTokenExpiresAtUtc.Should().Be(
            _timeProvider.GetUtcNow().UtcDateTime.AddDays(7));

        _refreshTokenRepositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<RefreshTokenSession>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenUserDoesNotExist()
    {
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var command = BuildCommand("x@y.com", "123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeNull();
        _refreshTokenRepositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<RefreshTokenSession>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        var email = "user@test.com";

        var user = new User
        {
            Email = email,
            PasswordHash = "hashed"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "wrong"))
            .Returns(PasswordVerificationResult.Failed);

        var handler = CreateHandler();
        var command = BuildCommand(email, "wrong");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeNull();
        _tokenGeneratorMock.Verify(
            t => t.Generate(It.IsAny<User>(), It.IsAny<bool>()),
            Times.Never);
        _refreshTokenRepositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<RefreshTokenSession>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenAdminMfaIsEnabled_AndSecondFactorIsMissing()
    {
        var email = "admin@test.com";
        var password = "StrongPassword#2026";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Admin",
            PasswordHash = "hashed"
        };
        user.MakeAdmin();
        user.ChangePassword("hashed");
        user.EnableAdminMfa(AdminMfaSecurity.GenerateSecretKey());

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, password))
            .Returns(PasswordVerificationResult.Success);

        var handler = CreateHandler();
        var command = BuildCommand(email, password);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeNull();
        _tokenGeneratorMock.Verify(
            t => t.Generate(It.IsAny<User>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnTokens_WhenAdminMfaCodeIsValid()
    {
        var email = "admin@test.com";
        var password = "StrongPassword#2026";
        var fakeAccessToken = "mfa-jwt-token";
        var secret = AdminMfaSecurity.GenerateSecretKey();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var validCode = AdminMfaSecurity.GenerateCurrentTotpCode(secret, now);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Admin",
            PasswordHash = "hashed"
        };
        user.MakeAdmin();
        user.ChangePassword("hashed");
        user.EnableAdminMfa(secret);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, password))
            .Returns(PasswordVerificationResult.Success);

        _tokenGeneratorMock
            .Setup(t => t.Generate(user, true))
            .Returns(fakeAccessToken);

        _refreshTokenRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RefreshTokenSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = BuildCommand(email, password, mfaCode: validCode);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be(fakeAccessToken);
        _tokenGeneratorMock.Verify(t => t.Generate(user, true), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldConsumeBackupCodeOnlyOnce_WhenAdminMfaIsEnabled()
    {
        var email = "admin@test.com";
        var password = "StrongPassword#2026";
        var fakeAccessToken = "backup-jwt-token";
        var secret = AdminMfaSecurity.GenerateSecretKey();
        var backupCode = "ABCD-EFGH";
        var backupHash = AdminMfaSecurity.HashBackupCode(backupCode);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Admin",
            PasswordHash = "hashed"
        };
        user.MakeAdmin();
        user.ChangePassword("hashed");
        user.EnableAdminMfa(secret);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, password))
            .Returns(PasswordVerificationResult.Success);

        _adminMfaRepositoryMock
            .SetupSequence(r => r.ConsumeBackupCodeAsync(user.Id, backupHash, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _tokenGeneratorMock
            .Setup(t => t.Generate(user, true))
            .Returns(fakeAccessToken);

        _refreshTokenRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RefreshTokenSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var firstResult = await handler.Handle(BuildCommand(email, password, backupCode: backupCode), CancellationToken.None);
        var secondResult = await handler.Handle(BuildCommand(email, password, backupCode: backupCode), CancellationToken.None);

        firstResult.Should().NotBeNull();
        secondResult.Should().BeNull();
    }

    private LoginUserCommandHandler CreateHandler()
    {
        return new LoginUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenGeneratorMock.Object,
            _refreshTokenRepositoryMock.Object,
            _adminMfaRepositoryMock.Object,
            _timeProvider,
            _options,
            _loggerMock.Object
        );
    }

    private static LoginUserCommand BuildCommand(string email, string password, string? mfaCode = null, string? backupCode = null)
    {
        return new LoginUserCommand(
            new LoginUserDto
            {
                Email = email,
                Password = password,
                MfaCode = mfaCode,
                BackupCode = backupCode
            },
            "127.0.0.1",
            "test-device");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
