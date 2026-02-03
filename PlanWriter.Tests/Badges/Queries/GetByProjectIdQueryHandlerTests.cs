using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Badges.Dtos.Queries;
using PlanWriter.Application.Badges.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Badges;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Badges.Queries;

public class GetByProjectIdQueryHandlerTests
{
    private readonly Mock<IBadgeRepository> _badgeRepositoryMock = new();
    private readonly Mock<ILogger<GetBadgesByProjectIdQueryHandler>> _loggerMock = new();
    private readonly Mock<IBadgeReadRepository> _badgeReadRepositoryMock = new();
    private readonly Mock<IBadgeRepository> _badgeRepository = new();

    private GetBadgesByProjectIdQueryHandler CreateHandler()
        => new(
            _badgeReadRepositoryMock.Object,
            _loggerMock.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnBadges_WhenBadgesExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var badges = new List<Badge>
        {
            new Badge { ProjectId = projectId, Name = "Primeiro Passo" },
            new Badge { ProjectId = projectId, Name = "Cem Palavras" }
        };

        _badgeReadRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(badges);

        var handler = CreateHandler();
        var query = new GetBadgesByProjectIdQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Name == "Primeiro Passo");
        result.Should().Contain(b => b.Name == "Cem Palavras");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoBadgesExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _badgeReadRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Badge>());

        var handler = CreateHandler();
        var query = new GetBadgesByProjectIdQuery(projectId, userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}