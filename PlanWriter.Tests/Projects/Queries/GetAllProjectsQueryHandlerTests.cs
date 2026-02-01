using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Application.Projects.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.ReadModels;
using PlanWriter.Infrastructure.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.Projects.Queries;

public class GetAllProjectsQueryHandlerTests
{
    private readonly Mock<IProjectReadRepository> _projectRepositoryMock = new();
    private readonly Mock<ILogger<GetAllProjectsQueryHandler>> _loggerMock = new();

    private GetAllProjectsQueryHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _projectRepositoryMock.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnProjects_ForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var projects = new List<ProjectDto>
        {
            new ProjectDto
            {
                Id = Guid.NewGuid(),
                Title = "Project A",
                CurrentWordCount = 1000,
                WordCountGoal = 5000
            },
            new ProjectDto
            {
                Id = Guid.NewGuid(),
                Title = "Project B",
                CurrentWordCount = 2000,
                WordCountGoal = 10000
            }
        };

        _projectRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(userId))
            .ReturnsAsync(projects);

        var handler = CreateHandler();
        var query = new GetAllProjectsQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);

        result[0].Title.Should().Be("Project A");
        result[0].WordCountGoal.Should().Be(5000);
        result[0].ProgressPercent.Should().BeApproximately(20, 0.01);

        result[1].Title.Should().Be("Project B");
        result[1].WordCountGoal.Should().Be(10000);
        result[1].ProgressPercent.Should().BeApproximately(20, 0.01);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoProjects()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(userId))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var query = new GetAllProjectsQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}