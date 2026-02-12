using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories;

public class ProjectProgressRepositoryTests
{
    [Fact]
    public async Task AddProgressAsync_ShouldReturnSameEntity()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new ProjectProgressRepository(db.Object);

        var entity = new ProjectProgress { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid(), WordsWritten = 500 };

        var result = await sut.AddProgressAsync(entity, CancellationToken.None);

        result.Should().BeSameAs(entity);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenAffectedRowsGreaterThanZero()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new ProjectProgressRepository(db.Object);

        var ok = await sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        ok.Should().BeTrue();
    }

    [Fact]
    public async Task GetByProjectAndDateRangeAsync_ShouldReturnRows()
    {
        var rows = new[] { new ProjectProgress { Id = Guid.NewGuid() } };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<ProjectProgress>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new ProjectProgressRepository(db.Object);

        var result = await sut.GetByProjectAndDateRangeAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, CancellationToken.None);

        result.Should().HaveCount(1);
    }
}
