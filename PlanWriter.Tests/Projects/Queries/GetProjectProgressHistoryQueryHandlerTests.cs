using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Application.Projects.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels;
using Xunit;

namespace PlanWriter.Tests.Projects.Queries;

public class GetProjectProgressHistoryQueryHandlerTests
{
    private readonly Mock<IProjectProgressReadRepository> _readRepo = new();
    private readonly Mock<ILogger<GetProjectProgressHistoryQueryHandler>> _logger = new();

    private GetProjectProgressHistoryQueryHandler CreateHandler()
        => new(_logger.Object, _readRepo.Object);

    [Fact]
    public async Task Handle_ShouldReturnOrderedProgressHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var rows = new List<ProgressHistoryRow>
        {
            new(new DateTime(2024, 1, 10), 500),
            new(new DateTime(2024, 1, 5),  300),
        };

        _readRepo
            .Setup(r => r.GetProgressHistoryAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var handler = CreateHandler();
        var query = new GetProjectProgressHistoryQuery(projectId, userId);

        // Act
        var resultEnumerable = await handler.Handle(query, CancellationToken.None);
        var result = new List<ProgressHistoryDto>(resultEnumerable);

        // Assert
        result.Should().HaveCount(2);
        result[0].Date.Should().Be(new DateTime(2024, 1, 5));
        result[1].Date.Should().Be(new DateTime(2024, 1, 10));

        _readRepo.Verify(
            r => r.GetProgressHistoryAsync(projectId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenRepositoryReturnsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _readRepo
            .Setup(r => r.GetProgressHistoryAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProgressHistoryRow>());

        var handler = CreateHandler();
        var query = new GetProjectProgressHistoryQuery(projectId, userId);

        // Act
        var resultEnumerable = await handler.Handle(query, CancellationToken.None);
        var result = new List<ProgressHistoryDto>(resultEnumerable);

        // Assert
        result.Should().BeEmpty();
    }
}
