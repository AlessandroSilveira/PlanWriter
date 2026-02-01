using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Badges.Dtos.Queries;
using PlanWriter.Application.Badges.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Badges.Queries;

public class GetByIdQueryHandlerTests
{
    private readonly Mock<IBadgeRepository> _badgeRepositoryMock = new();
    private readonly Mock<ILogger<GetByIdQueryHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldReturnBadges_WhenRepositoryReturnsBadges()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var badges = new List<Badge>
        {
            new Badge
            {
                Id = 1,
                Name = "Primeiro Badge"
            },
            new Badge
            {
                Id = 2,
                Name = "Segundo Badge"
            }
        };

        _badgeRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId))
            .ReturnsAsync(badges);

        var handler = new GetByIdQueryHandler(
            _badgeRepositoryMock.Object,
            _loggerMock.Object
        );

        var query = new GetByIdQuery(projectId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(badges);

        _badgeRepositoryMock.Verify(
            r => r.GetByProjectIdAsync(projectId),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenRepositoryReturnsNoBadges()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _badgeRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId))
            .ReturnsAsync(Enumerable.Empty<Badge>());

        var handler = new GetByIdQueryHandler(
            _badgeRepositoryMock.Object,
            _loggerMock.Object
        );

        var query = new GetByIdQuery(projectId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _badgeRepositoryMock.Verify(
            r => r.GetByProjectIdAsync(projectId),
            Times.Once
        );
    }
}