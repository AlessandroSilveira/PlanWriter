using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Commands;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Projects.Commands;

public class CreateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<ILogger<CreateProjectCommandHandler>> _loggerMock = new();

    private CreateProjectCommandHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _projectRepositoryMock.Object
        );

    [Fact]
    public async Task Handle_ShouldCreateProject_WhenRequestIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var request = new CreateProjectDto
        {
            Title = "My Project",
            Description = "Description",
            Genre = "Fantasy",
            WordCountGoal = 50000
        };

        var command = new CreateProjectCommand(request,userId);

        _projectRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<Project>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("My Project");
        result.WordCountGoal.Should().Be(50000);
        result.ProgressPercent.Should().Be(0);

        _projectRepositoryMock.Verify(
            r => r.CreateAsync(It.Is<Project>(p =>
                p.UserId == userId &&
                p.Title == "My Project" &&
                p.GoalUnit == GoalUnit.Words &&
                p.WordCountGoal == 50000 &&
                p.CurrentWordCount == 0
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldTrimTitleAndDescription()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var request = new CreateProjectDto()
        {
            Title = "  My Project  ",
            Description = "  Some description  ",
            WordCountGoal = 1000
        };

        var command = new CreateProjectCommand(request, userId);

        _projectRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<Project>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Title.Should().Be("My Project");
        result.Description.Should().Be("Some description");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTitleIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var request = new CreateProjectDto
        {
            Title = "   ",
            WordCountGoal = 1000
        };

        var command = new CreateProjectCommand(request, userId);

        var handler = CreateHandler();

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Title is required.");

        _projectRepositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldSetStartDateToNow_WhenNotProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var request = new CreateProjectDto
        {
            Title = "Project",
            WordCountGoal = 1000,
            StartDate = null
        };

        var command = new CreateProjectCommand(request, userId);

        ProjectDto? createdProject = null;

        _projectRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            
            .ReturnsAsync(It.IsAny<Project>());

        var handler = CreateHandler();

        // Act
        createdProject =  await handler.Handle(command, CancellationToken.None);

        // Assert
        createdProject.Should().NotBeNull(); createdProject!.StartDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(2)
        );
    }

    [Fact]
    public async Task Handle_ShouldResolveGoalTarget_WhenWordCountGoalIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var request = new CreateProjectDto
        {
            Title = "Project",
            WordCountGoal = null
        };

        var command = new CreateProjectCommand( request, userId);

        _projectRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<Project>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.WordCountGoal.Should().BeNull();
        result.ProgressPercent.Should().Be(0);
    }
}