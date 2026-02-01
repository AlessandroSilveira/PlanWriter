using Moq;
using PlanWriter.Application.Badges.Handlers;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Badges.Handlers;

public class AssignBadgesOnProgressHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IProjectProgressRepository> _progressRepo = new();
    private readonly Mock<IBadgeRepository> _badgeRepo = new();

    private AssignBadgesOnProgressHandler CreateHandler()
        => new(_projectRepo.Object, _progressRepo.Object, _badgeRepo.Object);

    [Fact]
    public async Task Handle_ShouldAssignFirstBadge_WhenFirstProgress()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _projectRepo
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId))
            .ReturnsAsync(new Project { Id = projectId, WordCountGoal = 1000 });

        _progressRepo
            .Setup(r => r.GetProgressByProjectIdAsync(projectId, userId))
            .ReturnsAsync(new List<ProjectProgress>
            {
                new ProjectProgress { WordsWritten = 50, Date = DateTime.UtcNow }
            });

        _badgeRepo
            .Setup(r => r.GetByProjectIdAsync(projectId))
            .ReturnsAsync(new List<Badge>());

        _badgeRepo
            .Setup(r => r.SaveAsync(It.IsAny<List<Badge>>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        await handler.Handle(
            new ProjectProgressAdded(projectId, userId, 50, Domain.Enums.GoalUnit.Words),
            CancellationToken.None
        );

        _badgeRepo.Verify(
            r => r.SaveAsync(It.Is<List<Badge>>(b => b.Count > 0)),
            Times.Once
        );
    }
}