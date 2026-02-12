using FluentAssertions;
using Moq;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories.DailyWordWrite;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories.DailyWordWrite;

public class DailyWordLogWriteRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_ShouldOnlyUpdate_WhenRowExists()
    {
        var db = new Mock<IDbExecutor>();
        db.SetupSequence(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new DailyWordLogWriteRepository(db.Object);

        await sut.UpsertAsync(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), 100, CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_ShouldInsert_WhenUpdateAffectsZeroRows()
    {
        var db = new Mock<IDbExecutor>();
        db.SetupSequence(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .ReturnsAsync(1);

        var sut = new DailyWordLogWriteRepository(db.Object);

        await sut.UpsertAsync(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), 100, CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByProjectAsync_ShouldReturnRows()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<PlanWriter.Domain.Dtos.Projects.DailyWordLogDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PlanWriter.Domain.Dtos.Projects.DailyWordLogDto
                {
                    Date = new DateOnly(2026, 1, 1),
                    WordsWritten = 50
                }
            });

        var sut = new DailyWordLogWriteRepository(db.Object);
        var result = await sut.GetByProjectAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(1);
    }
}
