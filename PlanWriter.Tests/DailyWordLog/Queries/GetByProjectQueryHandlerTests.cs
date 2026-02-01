using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.DailyWordLogs.Dtos.Queries;
using PlanWriter.Application.DailyWordLogs.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.DailyWordLog.Queries;

public class GetByProjectQueryHandlerTests
{
    private readonly Mock<IDailyWordLogRepository> _dailyWordLogRepoMock = new();
    private readonly Mock<ILogger<GetByProjectQueryHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldReturnDailyWordLogs_WhenLogsExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        var logs = new List<Domain.Entities.DailyWordLog>
        {
            new Domain.Entities.DailyWordLog
            {
                ProjectId = projectId,
                UserId = userId,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                WordsWritten = 500
            },
            new Domain.Entities.DailyWordLog
            {
                ProjectId = projectId,
                UserId = userId,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                WordsWritten = 800
            }
        };

        _dailyWordLogRepoMock
            .Setup(r => r.GetByProjectAsync(projectId, userId))
            .ReturnsAsync(logs);

        var handler = CreateHandler();
        var query = new GetByProjectQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().HaveCount(2);

        result.Should().BeEquivalentTo(new[]
        {
            new DailyWordLogDto
            {
                Date = logs[0].Date,
                WordsWritten = 500
            },
            new DailyWordLogDto
            {
                Date = logs[1].Date,
                WordsWritten = 800
            }
        });
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoLogsExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _dailyWordLogRepoMock
            .Setup(r => r.GetByProjectAsync(projectId, userId))
            .ReturnsAsync(new List<Domain.Entities.DailyWordLog>());

        var handler = CreateHandler();
        var query = new GetByProjectQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }



    private GetByProjectQueryHandler CreateHandler()
    {
        return new GetByProjectQueryHandler(
            _dailyWordLogRepoMock.Object,
            _loggerMock.Object
        );
    }
}