using FluentAssertions;
using Moq;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories.Auth.Register;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories.Auth.Register;

public class UserRegistrationReadRepositoryTests
{
    [Fact]
    public async Task EmailExistsAsync_ShouldReturnTrue_WhenRowExists()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new UserRegistrationReadRepository(db.Object);

        var result = await sut.EmailExistsAsync("user@test.com", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnFalse_WhenRowDoesNotExist()
    {
        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryFirstOrDefaultAsync<int?>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = new UserRegistrationReadRepository(db.Object);

        var result = await sut.EmailExistsAsync("user@test.com", CancellationToken.None);

        result.Should().BeFalse();
    }
}
