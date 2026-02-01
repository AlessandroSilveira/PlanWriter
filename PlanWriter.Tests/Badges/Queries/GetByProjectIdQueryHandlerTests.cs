using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Badges.Dtos.Queries;
using PlanWriter.Application.Badges.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Badges.Queries;

public class GetByProjectIdQueryHandlerTests
{
    private readonly Mock<IBadgeRepository> _badgeRepositoryMock = new();
    private readonly Mock<ILogger<GetByProjectIdQueryHandler>> _loggerMock = new();

    private GetByProjectIdQueryHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _badgeRepositoryMock.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnBadges_WhenBadgesExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var badges = new List<Badge>
        {
            new Badge { ProjectId = projectId, Name = "Primeiro Passo" },
            new Badge { ProjectId = projectId, Name = "Cem Palavras" }
        };

        _badgeRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId))
            .ReturnsAsync(badges);

        var handler = CreateHandler();
        var query = new GetByProjectIdQuery(projectId);

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

        _badgeRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId))
            .ReturnsAsync(new List<Badge>());

        var handler = CreateHandler();
        var query = new GetByProjectIdQuery(projectId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}