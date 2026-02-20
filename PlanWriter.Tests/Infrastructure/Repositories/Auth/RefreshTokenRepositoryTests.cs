using FluentAssertions;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Repositories.Auth;
using PlanWriter.Tests.Infrastructure;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories.Auth;

public class RefreshTokenRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldInsertRefreshTokenSession()
    {
        object? capturedParam = null;
        string? capturedSql = null;

        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return Task.FromResult(1);
            }
        };

        var sut = new RefreshTokenRepository(db);
        var session = new RefreshTokenSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            TokenHash = "abc",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        await sut.CreateAsync(session, CancellationToken.None);

        capturedSql.Should().Contain("INSERT INTO RefreshTokenSessions");
        capturedParam.Should().BeSameAs(session);
    }

    [Fact]
    public async Task GetByHashAsync_ShouldQueryUsingTokenHash()
    {
        object? capturedParam = null;
        string? capturedSql = null;
        var expected = new RefreshTokenSession { Id = Guid.NewGuid(), TokenHash = "hash" };

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (type, sql, param, _) =>
            {
                capturedSql = sql;
                capturedParam = param;
                return expected;
            }
        };

        var sut = new RefreshTokenRepository(db);
        var result = await sut.GetByHashAsync("hash", CancellationToken.None);

        result.Should().Be(expected);
        capturedSql.Should().Contain("FROM RefreshTokenSessions");
        capturedParam!.GetProp<string>("TokenHash").Should().Be("hash");
    }

    [Fact]
    public async Task MarkRotatedAsync_ShouldReturnTrue_WhenOneRowAffected()
    {
        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (_, _, _) => Task.FromResult(1)
        };

        var sut = new RefreshTokenRepository(db);
        var result = await sut.MarkRotatedAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeFamilyAsync_ShouldExecuteUpdate()
    {
        object? capturedParam = null;

        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (_, param, _) =>
            {
                capturedParam = param;
                return Task.FromResult(3);
            }
        };

        var sut = new RefreshTokenRepository(db);
        var familyId = Guid.NewGuid();
        var affected = await sut.RevokeFamilyAsync(
            familyId,
            DateTime.UtcNow,
            "reason",
            CancellationToken.None);

        affected.Should().Be(3);
        capturedParam!.GetProp<Guid>("FamilyId").Should().Be(familyId);
    }
}
