using FluentAssertions;
using Moq;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories.Auth;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories.Auth;

public class AuthAuditRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldInsertAuditRow()
    {
        object? capturedParam = null;
        string? capturedSql = null;

        var db = new Mock<IDbExecutor>();
        db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
            })
            .ReturnsAsync(1);

        var sut = new AuthAuditRepository(db.Object);

        await sut.CreateAsync(
            Guid.NewGuid(),
            "Login",
            "Success",
            "127.0.0.1",
            "tests",
            "trace",
            "correlation",
            null,
            CancellationToken.None);

        capturedSql.Should().Contain("INSERT INTO AuthAuditLogs");
        capturedParam.Should().NotBeNull();
    }
}
