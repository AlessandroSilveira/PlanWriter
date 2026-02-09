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
using PlanWriter.Domain.Interfaces.Repositories;
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

    [Fact]
    public async Task Handle_ShouldChangePasswordAndReturnToken_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPassword = "NewStrongPassword!";
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
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenPasswordIsTooShort()
    {
        var handler = CreateHandler();
        var command = BuildCommand(Guid.NewGuid(), "123");

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Password must have at least 6 characters.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var command = BuildCommand(userId, "Password123!");

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("User not found");
    }

    /* ===================== HELPERS ===================== */

    private ChangePasswordCommandHandler CreateHandler()
    {
        return new ChangePasswordCommandHandler(
            _userRepositoryMock.Object,
            _userPasswordRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenGeneratorMock.Object,
            _loggerMock.Object
        );
    }

    private static ChangePasswordCommand BuildCommand(Guid userId, string newPassword)
    {
        return new ChangePasswordCommand(userId, new ChangePasswordDto { NewPassword = newPassword });
    }
}