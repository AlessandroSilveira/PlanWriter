using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Auth.Commands;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Admin.Commands;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRegistrationReadRepository> _userRegistrationReadRepositoryMock = new();
    private readonly Mock<IUserRegistrationRepository> _userRegistrationRepositoryMock = new();
    private readonly Mock<ILogger<RegisterUserCommandHandler>> _loggerMock = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock = new();

    [Fact]
    public async Task Handle_ShouldThrow_WhenEmailAlreadyExists()
    {
        // Arrange
        var email = "user@planwriter.com";

        var existingUser = new User
        {
            Email = email
        };

        _userRegistrationReadRepositoryMock
            .Setup(r => r.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = BuildCommand(email);
        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"Register failed: email {existingUser.Email} already exists");

        _userRegistrationRepositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateRegularUser_WhenEmailDoesNotExist()
    {
        // Arrange
        var email = "newuser@planwriter.com";

        _userRegistrationReadRepositoryMock
            .Setup(r => r.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRegistrationRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = BuildCommand(email);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

      
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var email = "error@planwriter.com";

        _userRegistrationReadRepositoryMock
            .Setup(r => r.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var command = BuildCommand(email);
        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("DB error");
    }

    /* ===================== HELPERS ===================== */

    private RegisterUserCommandHandler CreateHandler()
    {
        return new RegisterUserCommandHandler(
            _userRegistrationReadRepositoryMock.Object,
            _userRegistrationRepositoryMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object
        );
    }

    private static RegisterUserCommand BuildCommand(string email)
    {
        return new RegisterUserCommand(
            new RegisterUserDto
            {
                Email = email,
                Password = "StrongPassword123!"
            }
        );
    }
}