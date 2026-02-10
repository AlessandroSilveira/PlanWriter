using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.DailyWordLogs.Dtos.Queries;
using PlanWriter.Application.DailyWordLogs.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.DailyWordLogWrite;
using Xunit;

namespace PlanWriter.Tests.DailyWordLog.Queries;

public class GetByProjectQueryHandlerTests
{
    private readonly Mock<IDailyWordLogReadRepository> _readRepositoryMock = new();
    private readonly Mock<ILogger<GetByProjectQueryHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldReturnDailyWordLogs_WhenLogsExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        var logs = new List<DailyWordLogDto>
        {
            new()
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                WordsWritten = 500
            },
            new()
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                WordsWritten = 800
            }
        };

        _readRepositoryMock
            .Setup(r => r.GetByProjectAsync(projectId, userId, ct))
            .ReturnsAsync(logs);

        var handler = CreateHandler();
        var query = new GetByProjectQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(logs);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoLogsExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _readRepositoryMock
            .Setup(r => r.GetByProjectAsync(projectId, userId, ct))
            .ReturnsAsync(new List<DailyWordLogDto>());

        var handler = CreateHandler();
        var query = new GetByProjectQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, ct);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /* ===================== HELPERS ===================== */

    private GetByProjectQueryHandler CreateHandler()
        => new(
            _readRepositoryMock.Object,
            _loggerMock.Object
        );
}
