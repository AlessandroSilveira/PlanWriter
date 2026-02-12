using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;
using PlanWriter.Tests.Infrastructure;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories;

public class DailyWordLogRepositoryTests
{
    [Fact]
    public async Task AddAsync_ShouldGenerateId_WhenEmpty()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new DailyWordLogRepository(db.Object);

        var log = new PlanWriter.Domain.Entities.DailyWordLog { Id = Guid.Empty, ProjectId = Guid.NewGuid(), UserId = Guid.NewGuid() };

        await sut.AddAsync(log);

        log.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task SumWordsAsync_ShouldSendDateOnlyRange()
    {
        var db = new Mock<IDbExecutor>();
        object? captured = null;
        db.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((_, p, _) => captured = p)
            .ReturnsAsync(123);

        var sut = new DailyWordLogRepository(db.Object);
        var start = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 20, 18, 0, 0, DateTimeKind.Utc);

        var result = await sut.SumWordsAsync(Guid.NewGuid(), start, end);

        result.Should().Be(123);
        captured.Should().NotBeNull();
        captured!.GetProp<DateOnly?>("Start").Should().Be(DateOnly.FromDateTime(start));
        captured.GetProp<DateOnly?>("End").Should().Be(DateOnly.FromDateTime(end));
    }

    [Fact]
    public async Task GetTotalWordsByUsersAsync_ShouldReturnEmpty_WhenNoIds()
    {
        var db = new Mock<IDbExecutor>();
        var sut = new DailyWordLogRepository(db.Object);

        var result = await sut.GetTotalWordsByUsersAsync(Array.Empty<Guid>(), null, null);

        result.Should().BeEmpty();
        db.Verify(x => x.QueryAsync<(Guid UserId, int Total)>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTotalWordsByUsersAsync_ShouldMapRowsToDictionary()
    {
        var userId = Guid.NewGuid();
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<(Guid UserId, int Total)>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { (userId, 77) });

        var sut = new DailyWordLogRepository(db.Object);

        var result = await sut.GetTotalWordsByUsersAsync(new[] { userId }, null, null);

        result[userId].Should().Be(77);
    }
}
