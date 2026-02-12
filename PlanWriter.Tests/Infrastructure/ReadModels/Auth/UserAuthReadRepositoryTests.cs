using FluentAssertions;
using Moq;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Auth;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Auth;

public class UserAuthReadRepositoryTests
{
    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser()
    {
        var expected = new User { Id = Guid.NewGuid(), Email = "admin@test.com" };
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new UserAuthReadRepository(db.Object);
        var result = await sut.GetByEmailAsync("admin@test.com", CancellationToken.None);

        result.Should().Be(expected);
    }
}
