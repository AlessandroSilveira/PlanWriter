using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Events;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Events;

public class EventReadRepositoryTests
{
    [Fact]
    public async Task GetActiveAsync_ShouldReturnRows()
    {
        var rows = new[]
        {
            new EventDto(Guid.NewGuid(), "NaNo", "nano", "Nanowrimo", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 50000, true)
        };

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<EventDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new EventReadRepository(db.Object);
        var result = await sut.GetActiveAsync(CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEventByIdAsync_ShouldReturnEvent()
    {
        var expected = new EventDto(Guid.NewGuid(), "NaNo", "nano", "Nanowrimo", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 50000, true);

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<EventDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new EventReadRepository(db.Object);
        var result = await sut.GetEventByIdAsync(expected.Id, CancellationToken.None);

        result.Should().Be(expected);
    }
}
