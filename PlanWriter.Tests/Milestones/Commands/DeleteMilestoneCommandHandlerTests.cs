using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Milestones.Commands;
using PlanWriter.Application.Milestones.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Milestones.Commands;

public class DeleteMilestoneCommandHandlerTests
{
    private readonly Mock<IMilestonesRepository> _milestonesRepositoryMock = new();
    private readonly Mock<ILogger<DeleteMilestoneCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldDeleteMilestone_WhenCalled()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var projectId = Guid.NewGuid();

        _milestonesRepositoryMock
            .Setup(r => r.DeleteAsync(milestoneId, userId))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new DeleteMilestoneCommand(projectId, milestoneId, userId);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.Should().Be(Unit.Value);

        _milestonesRepositoryMock.Verify(
            r => r.DeleteAsync(milestoneId, userId),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _milestonesRepositoryMock
            .Setup(r => r.DeleteAsync(milestoneId, userId))
            .ThrowsAsync(new Exception("DB error"));

        var handler = CreateHandler();
        var command = new DeleteMilestoneCommand(projectId,milestoneId, userId);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, ct);

        // Assert
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("DB error");
    }

    /* ===================== HELPERS ===================== */

    private DeleteMilestoneCommandHandler CreateHandler()
    {
        return new DeleteMilestoneCommandHandler(
            _loggerMock.Object,
            _milestonesRepositoryMock.Object
        );
    }
}