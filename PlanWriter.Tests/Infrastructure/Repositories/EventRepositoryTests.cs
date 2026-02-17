using System.Collections;
using System.Reflection;
using FluentAssertions;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Events;
using PlanWriter.Infrastructure.Repositories;
using PlanWriter.Tests.Infrastructure;
using Xunit;

namespace PlanWriter.Tests.Infrastructure.Repositories;

public class EventRepositoryTests
{
    [Fact]
    public async Task GetActiveEvents_ShouldMapTypeEnumToString()
    {
        var now = DateTime.UtcNow;
        var row = CreateEventRow(Guid.NewGuid(), "NaNo", "nano", (int)EventType.Nanowrimo, now.AddDays(-1), now.AddDays(1), 50000, true);

        var db = new StubDbExecutor
        {
            QueryAsyncHandler = (t, _, _, _) =>
            {
                if (t.Name != "EventRow")
                    return Array.Empty<object>();

                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t))!;
                list.Add(row);
                return list;
            }
        };

        var sut = new EventRepository(db);

        var result = await sut.GetActiveEvents();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(EventType.Nanowrimo.ToString());
    }

    [Fact]
    public async Task GetEventBySlug_ShouldReturnTrue_WhenCountIsPositive()
    {
        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) => t == typeof(int) ? 1 : null
        };

        var sut = new EventRepository(db);

        var exists = await sut.GetEventBySlug("nano");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AddEvent_ShouldGenerateId_WhenEntityIdIsEmpty()
    {
        object? captured = null;
        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (_, p, _) =>
            {
                captured = p;
                return Task.FromResult(1);
            }
        };

        var sut = new EventRepository(db);
        var ev = new Event
        {
            Id = Guid.Empty,
            Name = "NaNo",
            Slug = "nano",
            Type = EventType.Nanowrimo,
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddDays(1),
            DefaultTargetWords = 50000,
            IsActive = true
        };

        await sut.AddEvent(ev);

        ev.Id.Should().NotBe(Guid.Empty);
        captured.Should().NotBeNull();
        captured!.GetProp<int>("Type").Should().Be((int)EventType.Nanowrimo);
    }

    [Fact]
    public async Task GetAllAsync_ShouldMapRowsToDtos()
    {
        var row = CreateEventRow(Guid.NewGuid(), "NaNo", "nano", (int)EventType.Nanowrimo, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 50000, true);

        var db = new StubDbExecutor
        {
            QueryAsyncHandler = (t, _, _, _) =>
            {
                if (t.Name != "EventRow")
                    return Array.Empty<object>();

                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t))!;
                list.Add(row);
                return list;
            }
        };

        var sut = new EventRepository(db);
        var result = await sut.GetAllAsync();

        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result[0].Slug.Should().Be("nano");
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnRowsFromDb()
    {
        var expected = new[]
        {
            new EventLeaderboardRowDto
            {
                ProjectId = Guid.NewGuid(),
                ProjectTitle = "Book",
                UserName = "Alice",
                Words = 1000,
                Percent = 20,
                Won = false
            }
        };

        var db = new StubDbExecutor
        {
            QueryAsyncHandler = (t, _, _, _) => t == typeof(EventLeaderboardRowDto) ? expected : Array.Empty<EventLeaderboardRowDto>()
        };

        var sut = new EventRepository(db);

        var result = await sut.GetLeaderboard(new Event { Id = Guid.NewGuid() }, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 10);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetEventById_ShouldReturnEntity()
    {
        var expected = new Event
        {
            Id = Guid.NewGuid(),
            Name = "NaNo",
            Slug = "nano",
            Type = EventType.Nanowrimo
        };

        var db = new StubDbExecutor
        {
            QueryFirstOrDefaultAsyncHandler = (t, _, _, _) => t == typeof(Event) ? expected : null
        };

        var sut = new EventRepository(db);
        var result = await sut.GetEventById(expected.Id);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task UpdateAsync_ShouldSendEventTypeAsInt()
    {
        object? captured = null;
        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (_, p, _) =>
            {
                captured = p;
                return Task.FromResult(1);
            }
        };

        var sut = new EventRepository(db);
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Name = "NaNo",
            Slug = "nano",
            Type = EventType.Desafio,
            StartsAtUtc = DateTime.UtcNow.AddDays(-1),
            EndsAtUtc = DateTime.UtcNow.AddDays(1),
            IsActive = true
        };

        await sut.UpdateAsync(ev, ev.Id);

        captured.Should().NotBeNull();
        captured!.GetProp<int>("Type").Should().Be((int)EventType.Desafio);
    }

    [Fact]
    public async Task DeleteAsync_ShouldExecuteDelete()
    {
        var db = new StubDbExecutor
        {
            ExecuteAsyncHandler = (_, _, _) => Task.FromResult(1)
        };

        var sut = new EventRepository(db);
        await sut.DeleteAsync(new Event { Id = Guid.NewGuid() });
    }

    [Fact]
    public async Task GetEventByUserId_ShouldReturnRows()
    {
        var rows = new[]
        {
            new MyEventDto
            {
                EventId = Guid.NewGuid(),
                EventName = "NaNo",
                ProjectId = Guid.NewGuid(),
                ProjectTitle = "Book"
            }
        };

        var db = new StubDbExecutor
        {
            QueryAsyncHandler = (t, _, _, _) => t == typeof(MyEventDto) ? rows : Array.Empty<MyEventDto>()
        };

        var sut = new EventRepository(db);
        var result = await sut.GetEventByUserId(Guid.NewGuid());

        result.Should().HaveCount(1);
        result[0].EventName.Should().Be("NaNo");
    }

    private static object CreateEventRow(
        Guid id,
        string name,
        string slug,
        int type,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        int? defaultTargetWords,
        bool isActive,
        DateTime? validationWindowStartsAtUtc = null,
        DateTime? validationWindowEndsAtUtc = null,
        string? allowedValidationSources = null)
    {
        var rowType = typeof(EventRepository).GetNestedType("EventRow", BindingFlags.NonPublic)!;
        return Activator.CreateInstance(
            rowType,
            id,
            name,
            slug,
            type,
            startsAtUtc,
            endsAtUtc,
            defaultTargetWords,
            isActive,
            validationWindowStartsAtUtc,
            validationWindowEndsAtUtc,
            allowedValidationSources)!;
    }
}
