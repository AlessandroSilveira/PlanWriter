using Moq;
using PlanWriter.Application.Badges.Handlers;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Badges;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Badges.Handlers;

public class AssignBadgesOnProgressHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IProjectProgressRepository> _progressRepo = new();
    private readonly Mock<IBadgeRepository> _badgeRepo = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepo = new();
    private readonly Mock<IProjectProgressReadRepository> _progressReadRepo = new();
    private readonly Mock<IBadgeReadRepository> _badgeReadRepo = new();

    private AssignBadgesOnProgressHandler CreateHandler()
        => new(_projectRepo.Object, 
            _progressRepo.Object, 
            _badgeRepo.Object, _projectReadRepo.Object, _progressReadRepo.Object, _badgeReadRepo.Object);

    [Fact]
    public async Task Handle_ShouldAssignFirstBadge_WhenFirstProgress()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _projectReadRepo
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, new CancellationToken()))
            .ReturnsAsync(new Project { Id = projectId, WordCountGoal = 1000 });

        _progressReadRepo
            .Setup(r => r.GetProgressByProjectIdAsync(projectId, userId))
            .ReturnsAsync(new List<ProjectProgress>
            {
                new ProjectProgress { WordsWritten = 50, Date = DateTime.UtcNow }
            });

        _badgeReadRepo
            .Setup(r => r.GetByProjectIdAsync(projectId, userId, new CancellationToken()))
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