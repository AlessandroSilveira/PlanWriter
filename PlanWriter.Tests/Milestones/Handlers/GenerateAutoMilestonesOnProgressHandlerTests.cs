using Moq;
using PlanWriter.Application.Milestones.Handlers;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Milestones.Handlers;

public class GenerateAutoMilestonesOnProgressHandlerTests
{
    private readonly Mock<IMilestonesRepository> _milestonesRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();

    private GenerateAutoMilestonesOnProgressHandler CreateHandler()
        => new(_milestonesRepo.Object, _projectRepo.Object);

    [Fact]
    public async Task Handle_ShouldCreateAutoMilestones_WhenNotExisting()
    {
        var projectId = Guid.NewGuid();

        _projectRepo
            .Setup(r => r.GetProjectById(projectId))
            .ReturnsAsync(new Project { Id = projectId, WordCountGoal = 1000 });

        _milestonesRepo
            .Setup(r => r.GetByProjectIdAsync(projectId))
            .ReturnsAsync(new List<Milestone>());

        _milestonesRepo
            .Setup(r => r.AddAsync(It.IsAny<Milestone>()))
            .ReturnsAsync(It.IsAny<Milestone>());

        var handler = CreateHandler();

        await handler.Handle(
            new ProjectProgressAdded(projectId, Guid.NewGuid(), 300, Domain.Enums.GoalUnit.Words),
            CancellationToken.None
        );

        _milestonesRepo.Verify(
            r => r.AddAsync(It.IsAny<Milestone>()),
            Times.AtLeastOnce
        );
    }
}