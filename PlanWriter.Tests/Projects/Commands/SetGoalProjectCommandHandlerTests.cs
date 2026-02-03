using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Commands;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Projects.Commands;

public class SetGoalProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ILogger<SetGoalProjectCommandHandler>> _logger = new();

    private SetGoalProjectCommandHandler CreateHandler()
        => new(_logger.Object, _projectRepo.Object);

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenRepositoryUpdates()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deadline = DateTime.Now.AddDays(10);

        var dto = new SetFlexibleGoalDto
        {
            GoalAmount = 50000,
            Deadline = deadline,

        };
           
        var cmd = new SetGoalProjectCommand(projectId, userId, dto);

        _projectRepo
            .Setup(r => r.SetGoalAsync(projectId, userId, dto.GoalAmount, dto.Deadline, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _projectRepo.Verify(
            r => r.SetGoalAsync(projectId, userId, dto.GoalAmount, dto.Deadline, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenProjectNotFoundOrNotOwned()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var dto = new SetFlexibleGoalDto
        {
            GoalAmount = 30000,
            Deadline = null
        };
        var cmd = new SetGoalProjectCommand(projectId, userId, dto);

        _projectRepo
            .Setup(r => r.SetGoalAsync(projectId, userId, dto.GoalAmount, dto.Deadline, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _projectRepo.Verify(
            r => r.SetGoalAsync(projectId, userId, dto.GoalAmount, dto.Deadline, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenGoalAmountIsInvalid()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
       
        var dto = new SetFlexibleGoalDto
        {
            GoalAmount = 0,
            Deadline = null
        };
        
        var cmd = new SetGoalProjectCommand(projectId, userId, dto);

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("GoalAmount must be greater than zero.");

        _projectRepo.Verify(
            r => r.SetGoalAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
