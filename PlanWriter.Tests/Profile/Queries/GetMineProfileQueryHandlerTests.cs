using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Profile.Dtos.Queries;
using PlanWriter.Application.Profile.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;
using System.Threading;

namespace PlanWriter.Tests.Profile.Queries;

public class GetMineProfileQueryHandlerTests
{
    private readonly Mock<IUserReadRepository> _userReadRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<ILogger<GetMineProfileQueryHandler>> _loggerMock = new();

    private GetMineProfileQueryHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _userReadRepositoryMock.Object,
            _projectRepositoryMock.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnProfileWithPublicProjects()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            DisplayName = "User Test",
            Bio = "Bio",
            AvatarUrl = "avatar.png",
            IsProfilePublic = true,
            Slug = "user-test"
        };

        var projects = new List<Project>
        {
            new Project { Id = Guid.NewGuid(), UserId = userId, IsPublic = true },
            new Project { Id = Guid.NewGuid(), UserId = userId, IsPublic = false },
            new Project { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IsPublic = true }
        };

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var handler = CreateHandler();
        var query = new GetMineProfileQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Email.Should().Be("user@test.com");
        result.DisplayName.Should().Be("User Test");
        result.PublicProjectIds.Should().HaveCount(1);
        result.PublicProjectIds.Should().Contain(projects[0].Id);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var query = new GetMineProfileQuery(userId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Usuário não encontrado.");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPublicProjects_WhenUserHasNone()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            IsProfilePublic = true
        };

        _userReadRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        var handler = CreateHandler();
        var query = new GetMineProfileQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.PublicProjectIds.Should().BeEmpty();
    }
}
