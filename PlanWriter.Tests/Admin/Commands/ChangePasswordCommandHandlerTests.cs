using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Auth.Commands;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using Xunit;
using IUserReadRepository = PlanWriter.Domain.Interfaces.ReadModels.Users.IUserReadRepository;

namespace PlanWriter.Tests.Admin.Commands;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUserReadRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<ILogger<ChangePasswordCommandHandler>> _loggerMock = new();
    private readonly Mock<IUserPasswordRepository> _userPasswordRepositoryMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly TimeProvider _timeProvider = new FixedTimeProvider(
        new DateTimeOffset(2026, 2, 20, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Handle_ShouldChangePasswordAndReturnToken_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPassword = "NewStrongPassword1!";
        var hashed = "hashed-password";
        var token = "fake-jwt";

        var user = new User { Id = userId, PasswordHash = "" };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.HashPassword(user, newPassword))
            .Returns(hashed);

        _userPasswordRepositoryMock
            .Setup(r => r.UpdatePasswordAsync(userId, hashed, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _refreshTokenRepositoryMock
            .Setup(r => r.RevokeAllByUserAsync(userId, _timeProvider.GetUtcNow().UtcDateTime, "PasswordChanged", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _tokenGeneratorMock
            .Setup(t => t.Generate(user))
            .Returns(token);

        var handler = CreateHandler();
        var command = BuildCommand(userId, newPassword);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(token);

        // ✅ Em Dapper, não faz sentido assertar o estado do objeto em memória:
        // user.PasswordHash.Should().Be(hashed);  // REMOVER

        _passwordHasherMock.Verify(
            h => h.HashPassword(user, newPassword),
            Times.Once
        );

        _userPasswordRepositoryMock.Verify(
            r => r.UpdatePasswordAsync(userId, hashed, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _tokenGeneratorMock.Verify(
            t => t.Generate(user),
            Times.Once
        );

        _refreshTokenRepositoryMock.Verify(
            r => r.RevokeAllByUserAsync(userId, _timeProvider.GetUtcNow().UtcDateTime, "PasswordChanged", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenPasswordIsWeak()
    {
        var handler = CreateHandler();
        var command = BuildCommand(Guid.NewGuid(), "123");

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("A senha deve ter pelo menos 12 caracteres.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var command = BuildCommand(userId, "ValidPassword#2026");

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task Handle_ShouldGenerateTokenWithMustChangePasswordFalse_ForAdminUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string newPassword = "NewStrongPassword1!";
        const string hashed = "hashed-password";
        const string token = "admin-jwt";

        var user = new User { Id = userId, PasswordHash = "" };
        user.MakeAdmin();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.HashPassword(user, newPassword))
            .Returns(hashed);

        _userPasswordRepositoryMock
            .Setup(r => r.UpdatePasswordAsync(userId, hashed, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _refreshTokenRepositoryMock
            .Setup(r => r.RevokeAllByUserAsync(userId, _timeProvider.GetUtcNow().UtcDateTime, "PasswordChanged", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _tokenGeneratorMock
            .Setup(t => t.Generate(It.Is<User>(u => u.IsAdmin && !u.MustChangePassword)))
            .Returns(token);

        var handler = CreateHandler();
        var command = BuildCommand(userId, newPassword);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(token);
        user.MustChangePassword.Should().BeFalse();

        _tokenGeneratorMock.Verify(
            t => t.Generate(It.Is<User>(u => u.IsAdmin && !u.MustChangePassword)),
            Times.Once
        );

        _refreshTokenRepositoryMock.Verify(
            r => r.RevokeAllByUserAsync(userId, _timeProvider.GetUtcNow().UtcDateTime, "PasswordChanged", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    /* ===================== HELPERS ===================== */

    private ChangePasswordCommandHandler CreateHandler()
    {
        return new ChangePasswordCommandHandler(
            _userRepositoryMock.Object,
            _userPasswordRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenGeneratorMock.Object,
            _refreshTokenRepositoryMock.Object,
            _timeProvider,
            _loggerMock.Object
        );
    }

    private static ChangePasswordCommand BuildCommand(Guid userId, string newPassword)
    {
        return new ChangePasswordCommand(userId, new ChangePasswordDto { NewPassword = newPassword });
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
