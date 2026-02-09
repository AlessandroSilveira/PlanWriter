using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Milestones.Commands;
using PlanWriter.Application.Milestones.Dtos.Queries;
using PlanWriter.Application.Milestones.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Milestones;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Milestones.Commands;

public class GetProjectMilestonesQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IMilestonesRepository> _milestonesRepositoryMock = new();
    private readonly Mock<IMilestonesReadRepository> _milestonesReadRepositoryMock = new();
    private readonly Mock<ILogger<GetProjectMilestonesQueryHandler>> _loggerMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepositoryMock = new();

    [Fact]
    public async Task Handle_ShouldReturnOrderedMilestones_WhenUserHasAccessToProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
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

        _milestonesReadRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
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

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
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

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId });

        _milestonesReadRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Milestone>());

        var handler = CreateHandler();
        var query = new GetProjectMilestonesQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /* ===================== HELPERS ===================== */

    private GetProjectMilestonesQueryHandler CreateHandler()
    {
        return new GetProjectMilestonesQueryHandler(
            _loggerMock.Object, 
            _milestonesReadRepositoryMock.Object, 
            _projectReadRepositoryMock.Object
            );
    }
}