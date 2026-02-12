using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories;

public class MilestonesRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldGenerateId_WhenEmpty()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new MilestonesRepository(db.Object);

        var milestone = new Milestone { Id = Guid.Empty, Name = "m1" };

        await sut.CreateAsync(milestone, CancellationToken.None);

        milestone.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task UpdateAsync_ShouldExecuteUpdate()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new MilestonesRepository(db.Object);

        await sut.UpdateAsync(new Milestone { Id = Guid.NewGuid(), Name = "m2" }, CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldExecuteDeleteWithUserId()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new MilestonesRepository(db.Object);

        await sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
