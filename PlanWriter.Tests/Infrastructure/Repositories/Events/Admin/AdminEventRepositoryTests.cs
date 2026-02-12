using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories.Events.Admin;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories.Events.Admin;

public class AdminEventRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenAffectedIsNotOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var sut = new AdminEventRepository(db.Object);

        var act = () => sut.CreateAsync(new Event { Id = Guid.NewGuid(), Name = "Nano" }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenAffectedIsNotOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);
        var sut = new AdminEventRepository(db.Object);

        var dto = new EventDto(Guid.NewGuid(), "A", "a", "Nanowrimo", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 50000, true);

        var act = () => sut.UpdateAsync(dto.Id, dto, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldSucceed_WhenAffectedIsOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new AdminEventRepository(db.Object);

        var dto = new EventDto(Guid.NewGuid(), "A", "a", "Nanowrimo", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 50000, true);

        await sut.DeleteAsync(dto, CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
