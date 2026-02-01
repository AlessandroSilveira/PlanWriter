using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Profile.Commands;
using PlanWriter.Application.Profile.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Requests;
using Xunit;

namespace PlanWriter.Tests.Profile.Commands;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<ILogger<UpdateProfileCommandHandler>> _loggerMock = new();

    private UpdateProfileCommandHandler CreateHandler()
        => new(
            _userRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _loggerMock.Object
        );

    [Fact]
    public async Task Handle_ShouldUpdateBasicProfileFields()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            DisplayName = "Old Name",
            Bio = "Old Bio",
            AvatarUrl = "old.png",
            IsProfilePublic = false
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(user))
            .Returns(Task.CompletedTask);

        _projectRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Project>());

        var command = new UpdateProfileCommand(
            userId,
            new UpdateMyProfileRequest(
                DisplayName: "New Name",
                Bio: " New Bio ",
                AvatarUrl: " new.png ",
                IsProfilePublic: true,
                Slug: null,
                PublicProjectIds: null
            )
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        user.DisplayName.Should().Be("New Name");
        user.Bio.Should().Be("New Bio");
        user.AvatarUrl.Should().Be("new.png");
        user.IsProfilePublic.Should().BeTrue();

        result.DisplayName.Should().Be("New Name");

        _userRepositoryMock.Verify(
            r => r.UpdateAsync(user),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldGenerateSlug_WhenMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            DisplayName = "John Doe",
            Slug = null
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.SlugExistsAsync(It.IsAny<string>(), userId))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(user))
            .Returns(Task.CompletedTask);

        _projectRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Project>());

        var command = new UpdateProfileCommand(
            userId,
            new UpdateMyProfileRequest(
                DisplayName: null,
                Bio: null,
                AvatarUrl: null,
                IsProfilePublic: null,
                Slug: null,
                PublicProjectIds: null
            )
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        user.Slug.Should().NotBeNullOrWhiteSpace();
        result.Slug.Should().Be(user.Slug);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenSlugIsInvalid()
    {
        var userId = Guid.NewGuid();

        var user = new User { Id = userId };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        var command = new UpdateProfileCommand(
            userId,
            new UpdateMyProfileRequest(
                DisplayName: null,
                Bio: null,
                AvatarUrl: null,
                IsProfilePublic: null,
                Slug: "!!!",
                PublicProjectIds: null
            )
        );

        var handler = CreateHandler();

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Slug inválido.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenSlugAlreadyExists()
    {
        var userId = Guid.NewGuid();

        var user = new User { Id = userId };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.SlugExistsAsync("meu-slug", userId))
            .ReturnsAsync(true);

        var command = new UpdateProfileCommand(
            userId,
            new UpdateMyProfileRequest(
                DisplayName: null,
                Bio: null,
                AvatarUrl: null,
                IsProfilePublic: null,
                Slug: "meu slug",
                PublicProjectIds: null
            )
        );

        var handler = CreateHandler();

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Este slug já está em uso.");
    }

    [Fact]
    public async Task Handle_ShouldUpdatePublicProjectsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var user = new User { Id = userId };

        var project1 = new Project { Id = Guid.NewGuid(), UserId = userId, IsPublic = false };
        var project2 = new Project { Id = Guid.NewGuid(), UserId = userId, IsPublic = true };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(user))
            .Returns(Task.CompletedTask);

        _projectRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Project> { project1, project2 });

        _projectRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Project>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateProfileCommand(
            userId,
            new UpdateMyProfileRequest(
                DisplayName: null,
                Bio: null,
                AvatarUrl: null,
                IsProfilePublic: null,
                Slug: null,
                PublicProjectIds: new[] { project1.Id }
            )
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        project1.IsPublic.Should().BeTrue();
        project2.IsPublic.Should().BeFalse();

        _projectRepositoryMock.Verify(
            r => r.UpdateAsync(project1),
            Times.Once
        );

        _projectRepositoryMock.Verify(
            r => r.UpdateAsync(project2),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        var command = new UpdateProfileCommand(
            userId,
            new UpdateMyProfileRequest(
                DisplayName: null,
                Bio: null,
                AvatarUrl: null,
                IsProfilePublic: null,
                Slug: null,
                PublicProjectIds: null
            )
        );

        var handler = CreateHandler();

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Usuário não encontrado.");
    }
}