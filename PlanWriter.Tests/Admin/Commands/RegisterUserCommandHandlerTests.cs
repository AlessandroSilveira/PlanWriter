using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Auth.Commands;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Admin.Commands;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
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

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(existingUser);

        var command = BuildCommand(email);
        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("E-mail já cadastrado.");

        _userRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<User>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateRegularUser_WhenEmailDoesNotExist()
    {
        // Arrange
        var email = "newuser@planwriter.com";

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        var command = BuildCommand(email);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _userRepositoryMock.Verify(
            r => r.AddAsync(It.Is<User>(u =>
                u.Email == email &&
                u.IsAdmin == false &&
                u.MustChangePassword == false
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var email = "error@planwriter.com";

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
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
            _userRepositoryMock.Object,
            _loggerMock.Object,
            _passwordHasherMock.Object
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