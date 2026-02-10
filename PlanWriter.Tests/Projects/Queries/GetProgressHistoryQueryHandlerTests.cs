using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Application.Projects.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.Projects.Queries;

public class GetProgressHistoryQueryHandlerTests
{
    private readonly Mock<IProjectProgressReadRepository> _progressRepositoryMock = new();
    private readonly Mock<ILogger<GetProjectProgressHistoryQueryHandler>> _loggerMock = new();

    private GetProjectProgressHistoryQueryHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _progressRepositoryMock.Object
        );

    
    [Fact]
    public async Task Handle_ShouldReturnOrderedProgressHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var history = new List<ProgressHistoryRow>
        {
            new(new DateTime(2024, 1, 10), 500),
            new(new DateTime(2024, 1, 5),300)
        };

        _progressRepositoryMock
            .Setup(r => r.GetProgressHistoryAsync(projectId, userId, new CancellationToken()))
            .ReturnsAsync(history);

        var handler = CreateHandler();
        var query = new GetProjectProgressHistoryQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(2);
        resultList[0].Date.Should().Be(new DateTime(2024, 1,5));
        resultList[1].Date.Should().Be(new DateTime(2024, 1, 10));
    }


    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoHistoryExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _progressRepositoryMock
            .Setup(r => r.GetProgressHistoryAsync(projectId, userId, new CancellationToken()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var query = new GetProjectProgressHistoryQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}