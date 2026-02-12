using FluentAssertions;
using Moq;
using PlanWriter.Domain.Events;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories.ProjectEvents;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories.ProjectEvents;

public class ProjectEventsRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldGenerateId_WhenEmpty()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new ProjectEventsRepository(db.Object);

        var entity = new ProjectEvent { Id = Guid.Empty, ProjectId = Guid.NewGuid(), EventId = Guid.NewGuid() };

        await sut.CreateAsync(entity, CancellationToken.None);

        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task RemoveByKeysAsync_ShouldReturnFalse_WhenNoRowsAffected()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sut = new ProjectEventsRepository(db.Object);

        var result = await sut.RemoveByKeysAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByProjectAndEventAsync_ShouldReturnEntity()
    {
        var expected = new ProjectEvent { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid(), EventId = Guid.NewGuid() };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<ProjectEvent>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new ProjectEventsRepository(db.Object);

        var result = await sut.GetByProjectAndEventAsync(expected.ProjectId!.Value, expected.EventId, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task UpdateProjectEvent_ShouldExecuteUpdate()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new ProjectEventsRepository(db.Object);
        await sut.UpdateProjectEvent(new ProjectEvent { Id = Guid.NewGuid(), TargetWords = 50000 }, CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByKeys_ShouldExecuteDelete()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new ProjectEventsRepository(db.Object);
        await sut.RemoveByKeys(Guid.NewGuid(), Guid.NewGuid());

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByKeysAsync_ShouldReturnTrue_WhenRowsAreDeleted()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var sut = new ProjectEventsRepository(db.Object);
        var result = await sut.RemoveByKeysAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeTrue();
    }
}
