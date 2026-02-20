using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlanWriter.Application.Auth.Commands;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.DTO;
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
            .Setup(t => t.Generate(user))
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
            t => t.Generate(It.IsAny<User>()),
            Times.Never);
        _refreshTokenRepositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<RefreshTokenSession>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private LoginUserCommandHandler CreateHandler()
    {
        return new LoginUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenGeneratorMock.Object,
            _refreshTokenRepositoryMock.Object,
            _timeProvider,
            _options,
            _loggerMock.Object
        );
    }

    private static LoginUserCommand BuildCommand(string email, string password)
    {
        return new LoginUserCommand(
            new LoginUserDto
            {
                Email = email,
                Password = password
            },
            "127.0.0.1",
            "test-device");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
