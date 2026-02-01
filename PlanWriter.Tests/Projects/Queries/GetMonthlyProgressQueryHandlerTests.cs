using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Application.Projects.Queries;
using PlanWriter.Domain.Interfaces.ReadModels;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Projects.Queries;

public class GetMonthlyProgressQueryHandlerTests
{
    private readonly Mock<IProjectProgressReadRepository> _readRepo = new();
    private readonly Mock<ILogger<GetMonthlyProgressQueryHandler>> _logger = new();
    

    private GetMonthlyProgressQueryHandler CreateHandler()
        => new(_logger.Object, _readRepo.Object);

    [Fact]
    public async Task Handle_ShouldReturnMonthlyTotal_WhenRepositoryReturnsTotal()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var now = DateTime.UtcNow;
        var expectedStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedEnd = expectedStart.AddMonths(1);

        _readRepo
            .Setup(r => r.GetMonthlyWordsAsync(
                userId,
                expectedStart,
                expectedEnd,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1234);

        var handler = CreateHandler();
        var query = new GetMonthlyProgressQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(1234);

        _readRepo.Verify(r => r.GetMonthlyWordsAsync(
                userId,
                expectedStart,
                expectedEnd,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task Handle_ShouldReturnZero_WhenRepositoryReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedStart = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedEnd = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        _readRepo
            .Setup(r => r.GetMonthlyWordsAsync(userId, expectedStart, expectedEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var query = new GetMonthlyProgressQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }
}
