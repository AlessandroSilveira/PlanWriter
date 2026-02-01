using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Commands;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Projects.Commands;

public class DeleteProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<ILogger<DeleteProjectCommandHandler>> _loggerMock = new();

    private DeleteProjectCommandHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _projectRepositoryMock.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenProjectIsDeleted()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(r => r.DeleteProjectAsync(projectId, userId))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var command = new DeleteProjectCommand(projectId, userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _projectRepositoryMock.Verify(
            r => r.DeleteProjectAsync(projectId, userId),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenProjectDoesNotExistOrUserHasNoAccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(r => r.DeleteProjectAsync(projectId, userId))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var command = new DeleteProjectCommand(projectId, userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _projectRepositoryMock.Verify(
            r => r.DeleteProjectAsync(projectId, userId),
            Times.Once
        );
    }
}