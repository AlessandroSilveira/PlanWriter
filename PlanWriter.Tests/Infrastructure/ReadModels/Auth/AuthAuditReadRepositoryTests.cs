using FluentAssertions;
using Moq;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Auth;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.ReadModels.Auth;

public class AuthAuditReadRepositoryTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnRowsFromDbExecutor()
    {
        var expected = new List<AuthAuditLogDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EventType = "Login",
                Result = "Success",
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.QueryAsync<AuthAuditLogDto>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new AuthAuditReadRepository(db.Object);
        var result = await sut.GetAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            null,
            "Login",
            "Success",
            100,
            CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }
}
