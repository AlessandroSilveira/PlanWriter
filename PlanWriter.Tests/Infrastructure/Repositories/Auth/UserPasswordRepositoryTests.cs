using FluentAssertions;
using Moq;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories.Auth;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories.Auth;

public class UserPasswordRepositoryTests
{
    [Fact]
    public async Task UpdatePasswordAsync_ShouldSucceed_WhenOneRowAffected()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new UserPasswordRepository(db.Object);

        await sut.UpdatePasswordAsync(Guid.NewGuid(), "hash", CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePasswordAsync_ShouldThrow_WhenAffectedIsNotOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = new UserPasswordRepository(db.Object);

        var act = () => sut.UpdatePasswordAsync(Guid.NewGuid(), "hash", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
