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
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<ILogger<DeleteProjectCommandHandler>> _logger = new();

    private DeleteProjectCommandHandler CreateHandler()
        => new(_logger.Object, _projectRepository.Object);

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenProjectIsDeleted()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _projectRepository
            .Setup(r => r.DeleteProjectAsync(projectId, userId, ct))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var command = new DeleteProjectCommand(projectId, userId);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.Should().BeTrue();

        _projectRepository.Verify(
            r => r.DeleteProjectAsync(projectId, userId, ct),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenProjectDoesNotExistOrIsNotOwned()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _projectRepository
            .Setup(r => r.DeleteProjectAsync(projectId, userId, ct))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var command = new DeleteProjectCommand(projectId, userId);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.Should().BeFalse();

        _projectRepository.Verify(
            r => r.DeleteProjectAsync(projectId, userId, ct),
            Times.Once
        );
    }
}
