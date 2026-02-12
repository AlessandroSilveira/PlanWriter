using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories;

public class BadgeRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ShouldSkipDbCall_WhenCollectionIsEmpty()
    {
        var db = new Mock<IDbExecutor>();
        var sut = new BadgeRepository(db.Object);

        await sut.SaveAsync(Array.Empty<Badge>());

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistBadges_WhenCollectionHasItems()
    {
        var db = new Mock<IDbExecutor>();
        object? captured = null;
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((_, p, _) => captured = p)
            .ReturnsAsync(1);

        var sut = new BadgeRepository(db.Object);
        var badges = new[]
        {
            new Badge { Name = "First Steps" },
            new Badge { Name = "Winner" }
        };

        await sut.SaveAsync(badges);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
        captured.Should().BeAssignableTo<IReadOnlyList<Badge>>();
        ((IReadOnlyList<Badge>)captured!).Should().HaveCount(2);
    }
}
