using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;
using PlanWriter.Tests.Infrastructure;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldSendUserToDb()
    {
        var db = new Mock<IDbExecutor>();
        object? captured = null;

        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((_, p, _) => captured = p)
            .ReturnsAsync(1);

        var sut = new UserRepository(db.Object);
        var user = new User { Id = Guid.NewGuid(), Email = "user@test.com" };

        await sut.CreateAsync(user, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.GetProp<Guid>("Id").Should().Be(user.Id);
        captured.GetProp<string>("Email").Should().Be("user@test.com");
    }

    [Fact]
    public async Task UpdateAsync_ShouldSendUserToDb()
    {
        var db = new Mock<IDbExecutor>();
        object? captured = null;

        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((_, p, _) => captured = p)
            .ReturnsAsync(1);

        var sut = new UserRepository(db.Object);
        var user = new User { Id = Guid.NewGuid(), DisplayName = "John" };

        await sut.UpdateAsync(user, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.GetProp<Guid>("Id").Should().Be(user.Id);
        captured.GetProp<string?>("DisplayName").Should().Be("John");
    }
}
