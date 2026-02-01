using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Events.Commands;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Events.Commands;

public class LeaveEventCommandHandlerTests
{
    private readonly Mock<IProjectEventsRepository> _projectEventsRepoMock = new();
    private readonly Mock<ILogger<LeaveEventCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldRemoveProjectEvent_WhenCalled()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _projectEventsRepoMock
            .Setup(r => r.RemoveByKeys(projectId, eventId))
            .ReturnsAsync(It.IsAny<bool>());

        var handler = CreateHandler();
        var command = new LeaveEventCommand(projectId, eventId);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.Should().Be(Unit.Value);

        _projectEventsRepoMock.Verify(r => r.RemoveByKeys(projectId, eventId), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenRepositoryThrows()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _projectEventsRepoMock
            .Setup(r => r.RemoveByKeys(projectId, eventId))
            .ThrowsAsync(new Exception("DB error"));

        var handler = CreateHandler();
        var command = new LeaveEventCommand(projectId, eventId);

        // Act
        Func<Task> act = async () => await handler.Handle(command, ct);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
    }

    private LeaveEventCommandHandler CreateHandler()
    {
        return new LeaveEventCommandHandler(_projectEventsRepoMock.Object, _loggerMock.Object);
    }
}