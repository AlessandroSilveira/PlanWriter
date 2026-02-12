using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Profile.Commands;
using PlanWriter.Application.Profile.Dtos.Commands;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Requests;
using Xunit;

namespace PlanWriter.Tests.Profile.Commands;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserReadRepository> _userReadRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepositoryMock = new();
    private readonly Mock<ILogger<UpdateProfileCommandHandler>> _loggerMock = new();
    

    private UpdateProfileCommandHandler CreateHandler()
        => new(
            _userReadRepositoryMock.Object,
            _userRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _loggerMock.Object,
            _projectReadRepositoryMock.Object
        );

    [Fact]
    public async Task Handle_ShouldUpdateBasicProfileFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        

        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            DisplayName = "Old Name",
            Bio = "Old Bio",
            AvatarUrl = "old.png",
            IsProfilePublic = false
        };

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProjectDto>());

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
            r => r.UpdateAsync(user, It.IsAny<CancellationToken>()),
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

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userReadRepositoryMock
            .Setup(r => r.SlugExistsAsync(It.IsAny<string>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProjectDto>());

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

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
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

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userReadRepositoryMock
            .Setup(r => r.SlugExistsAsync("meu-slug", userId, It.IsAny<CancellationToken>()))
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

        var project1 = new ProjectDto { Id = Guid.NewGuid() };
        var project2 = new ProjectDto { Id = Guid.NewGuid() };

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                project1,
                project2
            });

        _projectRepositoryMock
            .Setup(r => r.SetProjectVisibilityAsync(It.IsAny<Guid>(), userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
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
        await handler.Handle(command, CancellationToken.None);

        // Assert

        _projectRepositoryMock.Verify(
            r => r.SetProjectVisibilityAsync(
                project1.Id,
                userId,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once
        );

        _projectRepositoryMock.Verify(
            r => r.SetProjectVisibilityAsync(
                project2.Id,
                userId,
                false,
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyPublicProjectIds()
    {
        var userId = Guid.NewGuid();
        var publicProjectId = Guid.NewGuid();
        var privateProjectId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@test.com"
        };

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProjectDto { Id = publicProjectId, IsPublic = true },
                new ProjectDto { Id = privateProjectId, IsPublic = false }
            });

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

        var result = await handler.Handle(command, CancellationToken.None);

        result.PublicProjectIds.Should().BeEquivalentTo(new[] { publicProjectId });
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
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
