using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Projects.Commands;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;
using AddProjectProgressDto = PlanWriter.Domain.Dtos.Projects.AddProjectProgressDto;

namespace PlanWriter.Tests.Projects.Commands;

public class AddProjectProgressCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IProjectProgressRepository> _progressRepo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<AddProjectProgressCommandHandler>> _logger = new ();
    private readonly Mock<IProjectReadRepository> _projectReadRepo = new();

    private AddProjectProgressCommandHandler CreateHandler()
        => new(_projectRepo.Object, _progressRepo.Object, _mediator.Object, _logger.Object, _projectReadRepo.Object);

    [Fact]
    public async Task Handle_ShouldAddProgress_AndPublishEvent()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            UserId = userId,
            GoalUnit = GoalUnit.Words,
            CurrentWordCount = 1000
        };

        _projectReadRepo
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _progressRepo
            .Setup(r => r.AddProgressAsync(It.IsAny<ProjectProgress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<ProjectProgress>());

        _projectRepo
            .Setup(r => r.UpdateAsync(project))
            .Returns(Task.CompletedTask);

        _mediator
            .Setup(m => m.Publish(It.IsAny<ProjectProgressAdded>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new AddProjectProgressCommand(
            project.Id,
            new AddProjectProgressDto
                {
                    ProjectId = projectId,
                WordsWritten = 200
            },
        userId
        );

        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        project.CurrentWordCount.Should().Be(1200);

        _mediator.Verify(
            m => m.Publish(
                It.Is<ProjectProgressAdded>(e =>
                    e.ProjectId == projectId &&
                    e.UserId == userId &&
                    e.NewTotal == 1200),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNoProgressProvided()
    {
        var handler = CreateHandler();

        var command = new AddProjectProgressCommand(
            Guid.NewGuid(),
            new  AddProjectProgressDto { ProjectId = Guid.NewGuid() },
            Guid.NewGuid()
        );

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }
}