using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories.Auth.Register;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories.Auth.Register;

public class UserRegistrationRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenAffectedIsOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new UserRegistrationRepository(db.Object);
        var user = new User { Id = Guid.NewGuid(), Email = "new@test.com" };

        await sut.CreateAsync(user, CancellationToken.None);

        db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenAffectedIsNotOne()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sut = new UserRegistrationRepository(db.Object);

        var act = () => sut.CreateAsync(new User { Id = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
