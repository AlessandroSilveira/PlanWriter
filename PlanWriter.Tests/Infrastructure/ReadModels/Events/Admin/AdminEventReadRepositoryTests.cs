using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Events.Admin;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Events.Admin;

public class AdminEventReadRepositoryTests
{
    [Fact]
    public async Task SlugExistsAsync_ShouldReturnTrue_WhenRowExists()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new AdminEventReadRepository(db.Object);

        (await sut.SlugExistsAsync("nano", CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEvent_WhenFound()
    {
        var expected = new EventDto(Guid.NewGuid(), "NaNo", "nano", "Nanowrimo", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 50000, true);
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<EventDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new AdminEventReadRepository(db.Object);
        var result = await sut.GetByIdAsync(expected.Id, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnRows()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<EventDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new EventDto(Guid.NewGuid(), "NaNo", "nano", "Nanowrimo", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 50000, true) });

        var sut = new AdminEventReadRepository(db.Object);
        var result = await sut.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(1);
    }
}
