using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Milestones;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Milestones;

public class MilestonesReadRepositoryTests
{
    [Fact]
    public async Task GetByProjectIdAsync_ShouldReturnOrderedRows()
    {
        var rows = new[]
        {
            new Milestone { Id = Guid.NewGuid(), Name = "m1" },
            new Milestone { Id = Guid.NewGuid(), Name = "m2" }
        };

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<Milestone>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new MilestonesReadRepository(db.Object);
        var result = await sut.GetByProjectIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNextOrderAsync_ShouldReturnLastPlusOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var sut = new MilestonesReadRepository(db.Object);
        var next = await sut.GetNextOrderAsync(Guid.NewGuid(), CancellationToken.None);

        next.Should().Be(4);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenNameIsWhitespace()
    {
        var db = new Mock<IDbExecutor>();
        var sut = new MilestonesReadRepository(db.Object);

        var exists = await sut.ExistsAsync(Guid.NewGuid(), "   ", CancellationToken.None);

        exists.Should().BeFalse();
        db.Verify(x => x.QueryFirstOrDefaultAsync<bool>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
