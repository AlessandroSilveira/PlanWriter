using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Badges;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Badges;

public class BadgeReadRepositoryTests
{
    [Fact]
    public async Task GetByProjectIdAsync_ShouldReturnRows()
    {
        var rows = new[] { new Badge { Id = 1, Name = "First" } };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<Badge>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(rows);

        var sut = new BadgeReadRepository(db.Object);
        var result = await sut.GetByProjectIdAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task HasFirstStepsBadgeAsync_ShouldReturnTrue_WhenRowExists()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new BadgeReadRepository(db.Object);

        (await sut.HasFirstStepsBadgeAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenNoRowExists()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = new BadgeReadRepository(db.Object);

        (await sut.ExistsAsync(Guid.NewGuid(), Guid.NewGuid(), "Winner", CancellationToken.None)).Should().BeFalse();
    }
}
