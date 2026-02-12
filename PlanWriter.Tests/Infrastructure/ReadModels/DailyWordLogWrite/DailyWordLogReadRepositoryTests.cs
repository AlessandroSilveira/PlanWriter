using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.DailyWordLogWrite;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.DailyWordLogWrite;

public class DailyWordLogReadRepositoryTests
{
    [Fact]
    public async Task GetByProjectAsync_ShouldReturnRows()
    {
        var rows = new[] { new DailyWordLogDto { Date = new DateOnly(2026, 1, 1), WordsWritten = 100 } };

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<DailyWordLogDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new DailyWordLogReadRepository(db.Object);
        var result = await sut.GetByProjectAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(1);
    }
}
