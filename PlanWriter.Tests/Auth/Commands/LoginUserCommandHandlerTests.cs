using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Auth.Commands;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Auth.Commands;

public class LoginUserCommandHandlerTests
{
    private readonly Mock<IUserAuthReadRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<ILogger<LoginUserCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "user@test.com";
        var password = "Password123";
        var fakeToken = "fake-jwt-token";

        var user = new User
        {
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
            .Returns(fakeToken);

        var handler = CreateHandler();
        var command = BuildCommand(email, password);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(fakeToken);

        _tokenGeneratorMock.Verify(
            t => t.Generate(user),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var command = BuildCommand("x@y.com", "123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        // Arrange
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

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _tokenGeneratorMock.Verify(
            t => t.Generate(It.IsAny<User>()),
            Times.Never
        );
    }

    /* ===================== HELPERS ===================== */

    private LoginUserCommandHandler CreateHandler()
    {
        return new LoginUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenGeneratorMock.Object,
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
            }
        );
    }
}