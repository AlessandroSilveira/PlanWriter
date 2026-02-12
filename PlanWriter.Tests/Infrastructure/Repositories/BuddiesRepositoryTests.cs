using System.Collections;
using System.Reflection;
using FluentAssertions;
using PlanWriter.Infrastructure.Repositories;
using PlanWriter.Tests.Infrastructure;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories;

public class BuddiesRepositoryTests
{
    [Fact]
    public async Task FindUserIdByUsernameAsync_ShouldReturnUserId_WhenFound()
    {
        var expectedId = Guid.NewGuid();
        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) => t == typeof(Guid?) ? (Guid?)expectedId : null
        };

        var sut = new BuddiesRepository(db);

        var result = await sut.FindUserIdByUsernameAsync("alice", CancellationToken.None);

        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task GetBuddySummariesAsync_ShouldReturnEmpty_WhenUserIdsAreEmpty()
    {
        var called = false;
        var db = new StubDbExecutor
        {
            QueryAsyncHandler = (_, _, _, _) =>
            {
                called = true;
                return Array.Empty<object>();
            }
        };

        var sut = new BuddiesRepository(db);
        var result = await sut.GetBuddySummariesAsync(Array.Empty<Guid>(), CancellationToken.None);

        result.Should().BeEmpty();
        called.Should().BeFalse();
    }

    [Fact]
    public async Task GetBuddySummariesAsync_ShouldMapRows()
    {
        var rowType = typeof(BuddiesRepository).GetNestedType("BuddyUserRow", BindingFlags.NonPublic)!;
        var row = Activator.CreateInstance(rowType,
            Guid.NewGuid(),
            "alice",
            "alice@test.com",
            "Alice",
            "Silva",
            "Alice S",
            "avatar.png")!;

        var db = new StubDbExecutor
        {
            QueryAsyncHandler = (t, _, _, _) =>
            {
                if (t != rowType)
                    return Array.Empty<object>();

                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t))!;
                list.Add(row);
                return list;
            }
        };

        var sut = new BuddiesRepository(db);
        var result = await sut.GetBuddySummariesAsync(new[] { Guid.NewGuid() }, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Username.Should().Be("alice");
        result[0].DisplayName.Should().Be("Alice S");
    }

    [Fact]
    public async Task GetEventWindowAsync_ShouldConvertDatesToDateOnly()
    {
        var starts = new DateTime(2026, 1, 10, 13, 0, 0, DateTimeKind.Utc);
        var ends = new DateTime(2026, 1, 20, 23, 0, 0, DateTimeKind.Utc);
        var rowType = typeof(BuddiesRepository).GetNestedType("EventWindowRow", BindingFlags.NonPublic)!;
        var row = Activator.CreateInstance(rowType, starts, ends)!;

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) => t == rowType ? row : null
        };

        var sut = new BuddiesRepository(db);

        var result = await sut.GetEventWindowAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Value.start.Should().Be(DateOnly.FromDateTime(starts));
        result.Value.end.Should().Be(DateOnly.FromDateTime(ends));
    }
}
