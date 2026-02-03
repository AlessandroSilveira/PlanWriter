using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Application.Projects.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Infrastructure.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.Projects.Queries;

public class GetProjectByIdQueryHandlerTests
{
    private readonly Mock<IProjectReadRepository> _projectRepositoryMock = new();
    private readonly Mock<ILogger<GetProjectByIdQueryHandler>> _loggerMock = new();

    private GetProjectByIdQueryHandler CreateHandler()
        => new(_loggerMock.Object, _projectRepositoryMock.Object);

    [Fact]
    public async Task Handle_ShouldReturnProject_WhenUserHasAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var project = new ProjectDto
        {
            Id = projectId,
            Title = "My Project",
            CurrentWordCount = 2500,
            WordCountGoal = 5000
        };

        _projectRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var handler = CreateHandler();
        var query = new GetProjectByIdQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(projectId);
        result.Title.Should().Be("My Project");
        result.WordCountGoal.Should().Be(5000);
        result.ProgressPercent.Should().BeApproximately(50, 0.01);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProjectDoesNotExistOrUserHasNoAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectDto?)null);

        var handler = CreateHandler();
        var query = new GetProjectByIdQuery(projectId, userId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Project not found");
    }

    [Fact]
    public async Task Handle_ShouldResolveGoalAmount_WhenWordCountGoalIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var project = new ProjectDto
        {
            Id = projectId,
            CurrentWordCount = 1000,
            WordCountGoal = 4000
        };

        _projectRepositoryMock
            .Setup(r => r.GetProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var handler = CreateHandler();
        var query = new GetProjectByIdQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.WordCountGoal.Should().Be(4000);
        result.ProgressPercent.Should().BeApproximately(25, 0.01);
    }
}