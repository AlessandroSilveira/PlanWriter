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
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<ILogger<SetGoalProjectCommandHandler>> _loggerMock = new();

    private SetGoalProjectCommandHandler CreateHandler()
        => new(_loggerMock.Object, _projectRepositoryMock.Object);

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenGoalIsSetSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var requestDto = new SetFlexibleGoalDto
        {
            GoalAmount = 50000,
            Deadline = DateTime.UtcNow.AddMonths(1)
        };

        var command = new SetGoalProjectCommand(
            projectId,
            userId,
            requestDto
        );

        _projectRepositoryMock
            .Setup(r => r.SetGoalAsync(
                projectId,
                userId,
                requestDto.GoalAmount,
                requestDto.Deadline))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _projectRepositoryMock.Verify(
            r => r.SetGoalAsync(
                projectId,
                userId,
                requestDto.GoalAmount,
                requestDto.Deadline),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenRepositoryReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var requestDto = new SetFlexibleGoalDto
        {
            GoalAmount = 10000,
            Deadline = null
        };

        var command = new SetGoalProjectCommand(
            projectId,
            userId,
            requestDto
        );

        _projectRepositoryMock
            .Setup(r => r.SetGoalAsync(
                projectId,
                userId,
                requestDto.GoalAmount,
                requestDto.Deadline))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _projectRepositoryMock.Verify(
            r => r.SetGoalAsync(
                projectId,
                userId,
                requestDto.GoalAmount,
                requestDto.Deadline),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var requestDto = new SetFlexibleGoalDto
        {
            GoalAmount = 5000,
            Deadline = DateTime.UtcNow
        };

        var command = new SetGoalProjectCommand(
            projectId,
            userId,
            requestDto
        );

        _projectRepositoryMock
            .Setup(r => r.SetGoalAsync(
                projectId,
                userId,
                requestDto.GoalAmount,
                requestDto.Deadline))
            .ThrowsAsync(new InvalidOperationException("Invalid goal"));

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid goal");
    }
}