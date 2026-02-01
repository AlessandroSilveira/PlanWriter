using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Milestones.Dtos.Queries;
using PlanWriter.Application.Milestones.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Milestones.Queries;

public class GetProjectMilestonesQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IMilestonesRepository> _milestonesRepositoryMock = new();
    private readonly Mock<ILogger<GetProjectMilestonesQueryHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldReturnOrderedMilestones_WhenUserHasAccessToProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _projectRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId))
            .ReturnsAsync(new Project { Id = projectId });

        var milestones = new List<Milestone>
        {
            new Milestone
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "Second",
                Order = 2
            },
            new Milestone
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "First",
                Order = 1
            }
        };

        _milestonesRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId))
            .ReturnsAsync(milestones);

        var handler = CreateHandler();
        var query = new GetProjectMilestonesQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("First");
        result[1].Name.Should().Be("Second");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserDoesNotHaveAccessToProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId))
            .ReturnsAsync((Project?)null);

        var handler = CreateHandler();
        var query = new GetProjectMilestonesQuery(projectId, userId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Projeto não encontrado.");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenProjectHasNoMilestones()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId))
            .ReturnsAsync(new Project { Id = projectId });

        _milestonesRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var query = new GetProjectMilestonesQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
   

    private GetProjectMilestonesQueryHandler CreateHandler()
    {
        return new GetProjectMilestonesQueryHandler(_loggerMock.Object, _projectRepositoryMock.Object, _milestonesRepositoryMock.Object);
    }
}