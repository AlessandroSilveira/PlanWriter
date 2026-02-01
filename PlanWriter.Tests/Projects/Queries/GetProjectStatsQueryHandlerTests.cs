using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Application.Projects.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.Projects.Queries;

public class GetProjectStatsQueryHandlerTests
{
    private readonly Mock<IProjectReadRepository> _projectRepo = new();
    private readonly Mock<IProjectProgressReadRepository> _progressRepo = new();
    private readonly Mock<ILogger<GetProjectStatsQueryHandler>> _logger = new();

    private GetProjectStatsQueryHandler CreateHandler()
        => new(_logger.Object, _projectRepo.Object, _progressRepo.Object);

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenProjectNotFound()
    {
        _projectRepo
            .Setup(r => r.GetUserProjectsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((List<ProjectDto>?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetProjectStatsQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyStats_WhenNoProgress()
    {
        var project = new ProjectDto
        {
            StartDate = DateTime.UtcNow.AddDays(-5),
            WordCountGoal = 1000
        };

        _projectRepo
            .Setup(r => r.GetProjectByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(project);

        _progressRepo
            .Setup(r => r.GetProgressByDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<ProjectProgressDayDto>());

        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetProjectStatsQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        result!.TotalWords.Should().Be(0);
        result.ActiveDays.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldCalculateStats_WhenProgressExists()
    {
        var project = new ProjectDto
        {
            StartDate = DateTime.UtcNow.AddDays(-5),
            WordCountGoal = 1000,
            GoalUnit = GoalUnit.Words
        };

        var entries = new List<ProjectProgressDayDto>
        {
            new(DateTime.UtcNow.AddDays(-2), 300 ,0,0),
            new(DateTime.UtcNow.AddDays(-1), 200,0,0 )
        };

        _projectRepo
            .Setup(r => r.GetProjectByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(project);

        _progressRepo
            .Setup(r => r.GetProgressByDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(entries);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetProjectStatsQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        result!.TotalWords.Should().Be(500);
        result.ActiveDays.Should().Be(2);
        result.AveragePerDay.Should().BeGreaterThan(0);
    }
}