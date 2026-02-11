using FluentAssertions;
using Moq;
using PlanWriter.Application.Milestones.Handlers;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Milestones;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Milestones.Handlers;

public class CompleteMilestonesOnProgressHandlerTests
{
    private readonly Mock<IMilestonesRepository> _repo = new();
    private readonly Mock<IMilestonesReadRepository> _milestonesReadRepository = new();

    private CompleteMilestonesOnProgressHandler CreateHandler()
        => new( _milestonesReadRepository.Object, _repo.Object);

    [Fact]
    public async Task Handle_ShouldCompleteMilestones_WhenTargetReached()
    {
        var projectId = Guid.NewGuid();

        var milestone = new Milestone
        {
            ProjectId = projectId,
            TargetAmount = 1000,
            Completed = false
        };

        _milestonesReadRepository
            .Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Milestone> { milestone });

        _repo.Setup(r => r.UpdateAsync(It.IsAny<Milestone>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        await handler.Handle(
            new ProjectProgressAdded(projectId, Guid.NewGuid(), 1500, Domain.Enums.GoalUnit.Words),
            CancellationToken.None
        );

        milestone.Completed.Should().BeTrue();
        milestone.CompletedAt.Should().NotBeNull();

        _repo.Verify(
            r => r.UpdateAsync(
                It.Is<Milestone>(m => m.ProjectId == projectId && m.Completed),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
}
