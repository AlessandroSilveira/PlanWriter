using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Users;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Users;

public class UserReadRepositoryTests
{
    [Fact]
    public async Task EmailExistsAsync_ShouldReturnTrue_WhenRowExists()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new UserReadRepository(db.Object);
        var exists = await sut.EmailExistsAsync("user@test.com", CancellationToken.None);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnFalse_WhenRowDoesNotExist()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = new UserReadRepository(db.Object);
        var exists = await sut.EmailExistsAsync("user@test.com", CancellationToken.None);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenFound()
    {
        var expected = new User { Id = Guid.NewGuid(), Email = "user@test.com" };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new UserReadRepository(db.Object);
        var result = await sut.GetByIdAsync(expected.Id, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenFound()
    {
        var expected = new User { Id = Guid.NewGuid(), Email = "user@test.com" };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new UserReadRepository(db.Object);
        var result = await sut.GetByEmailAsync(expected.Email, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task SlugExistsAsync_ShouldReturnTrue_WhenSlugExistsForAnotherUser()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new UserReadRepository(db.Object);
        var result = await sut.SlugExistsAsync("alice", Guid.NewGuid(), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnUser_WhenFound()
    {
        var expected = new User { Id = Guid.NewGuid(), Slug = "alice" };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new UserReadRepository(db.Object);
        var result = await sut.GetBySlugAsync("alice", CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetUsersByIdsAsync_ShouldReturnRows()
    {
        var rows = new[] { new User { Id = Guid.NewGuid() }, new User { Id = Guid.NewGuid() } };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<User>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new UserReadRepository(db.Object);
        var result = await sut.GetUsersByIdsAsync(rows.Select(x => x.Id), CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
