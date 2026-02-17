using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PlanWriter.Application.Events.Commands;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Tests.API.Integration;
using Xunit;

namespace PlanWriter.Tests.Events.Integration;

public class EventFinalizeFallbackIntegrationTests
{
    [Fact]
    public async Task JoinAndFinalize_ShouldUseEventDefaultTarget_WhenRequestTargetIsNull()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var profileStore = new InMemoryProfileStore();
        var projectRepository = new InMemoryProjectRepository(profileStore);
        profileStore.SeedProject(new Project
        {
            Id = projectId,
            UserId = userId,
            Title = "Projeto Integração",
            GoalAmount = 100000,
            GoalUnit = GoalUnit.Words,
            WordCountGoal = 100000,
            StartDate = DateTime.UtcNow.Date
        });

        var store = new EventFlowStore();
        store.Events[eventId] = new Event
        {
            Id = eventId,
            Name = "Evento de Teste",
            Slug = "evento-teste",
            Type = EventType.Nanowrimo,
            StartsAtUtc = DateTime.UtcNow.AddDays(-1),
            EndsAtUtc = DateTime.UtcNow.AddDays(1),
            DefaultTargetWords = 42000,
            IsActive = true
        };
        store.ProgressEntries.Add(new ProjectProgress
        {
            ProjectId = projectId,
            Date = DateTime.UtcNow,
            WordsWritten = 43000
        });

        var eventRepository = new InMemoryEventRepository(store);
        var eventReadRepository = new InMemoryEventReadRepository(store);
        var projectEventsRepository = new InMemoryProjectEventsRepository(store);
        var projectEventsReadRepository = new InMemoryProjectEventsReadRepository(store);
        var projectProgressRepository = new InMemoryProjectProgressRepository(store);
        var badgeRepository = new InMemoryBadgeRepository(store);

        var joinHandler = new JoinEventCommandHandler(
            eventRepository,
            projectRepository,
            projectEventsRepository,
            projectEventsReadRepository,
            NullLogger<JoinEventCommandHandler>.Instance,
            eventReadRepository,
            projectRepository);

        var joined = await joinHandler.Handle(
            new JoinEventCommand(new JoinEventRequest(projectId, eventId, null), userId),
            CancellationToken.None);

        joined.TargetWords.Should().Be(42000);

        var finalizeHandler = new FinalizeEventCommandHandler(
            projectEventsRepository,
            eventRepository,
            projectProgressRepository,
            badgeRepository,
            NullLogger<FinalizeEventCommandHandler>.Instance,
            projectEventsReadRepository);

        var finalized = await finalizeHandler.Handle(
            new FinalizeEventCommand(new FinalizeRequest(joined.Id)),
            CancellationToken.None);

        finalized.Won.Should().BeTrue();
        finalized.FinalWordCount.Should().Be(43000);
        store.SavedBadges.Should().ContainSingle(b => b.Icon == "🏆" && b.Description.Contains("42.000"));
    }

    [Fact]
    public async Task JoinAndFinalize_ShouldFallbackToGlobalDefaultTarget_WhenEventDefaultIsNull()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var profileStore = new InMemoryProfileStore();
        var projectRepository = new InMemoryProjectRepository(profileStore);
        profileStore.SeedProject(new Project
        {
            Id = projectId,
            UserId = userId,
            Title = "Projeto Integração",
            GoalAmount = 100000,
            GoalUnit = GoalUnit.Words,
            WordCountGoal = 100000,
            StartDate = DateTime.UtcNow.Date
        });

        var store = new EventFlowStore();
        store.Events[eventId] = new Event
        {
            Id = eventId,
            Name = "Evento sem default",
            Slug = "evento-sem-default",
            Type = EventType.Nanowrimo,
            StartsAtUtc = DateTime.UtcNow.AddDays(-1),
            EndsAtUtc = DateTime.UtcNow.AddDays(1),
            DefaultTargetWords = null,
            IsActive = true
        };
        store.ProgressEntries.Add(new ProjectProgress
        {
            ProjectId = projectId,
            Date = DateTime.UtcNow,
            WordsWritten = 50000
        });

        var eventRepository = new InMemoryEventRepository(store);
        var eventReadRepository = new InMemoryEventReadRepository(store);
        var projectEventsRepository = new InMemoryProjectEventsRepository(store);
        var projectEventsReadRepository = new InMemoryProjectEventsReadRepository(store);
        var projectProgressRepository = new InMemoryProjectProgressRepository(store);
        var badgeRepository = new InMemoryBadgeRepository(store);

        var joinHandler = new JoinEventCommandHandler(
            eventRepository,
            projectRepository,
            projectEventsRepository,
            projectEventsReadRepository,
            NullLogger<JoinEventCommandHandler>.Instance,
            eventReadRepository,
            projectRepository);

        var joined = await joinHandler.Handle(
            new JoinEventCommand(new JoinEventRequest(projectId, eventId, null), userId),
            CancellationToken.None);

        joined.TargetWords.Should().Be(50000);

        var finalizeHandler = new FinalizeEventCommandHandler(
            projectEventsRepository,
            eventRepository,
            projectProgressRepository,
            badgeRepository,
            NullLogger<FinalizeEventCommandHandler>.Instance,
            projectEventsReadRepository);

        var finalized = await finalizeHandler.Handle(
            new FinalizeEventCommand(new FinalizeRequest(joined.Id)),
            CancellationToken.None);

        finalized.Won.Should().BeTrue();
        finalized.FinalWordCount.Should().Be(50000);
        store.SavedBadges.Should().ContainSingle(b => b.Icon == "🏆" && b.Description.Contains("50.000"));
    }

    private sealed class EventFlowStore
    {
        public Dictionary<Guid, Event> Events { get; } = [];
        public Dictionary<Guid, ProjectEvent> ProjectEvents { get; } = [];
        public List<ProjectProgress> ProgressEntries { get; } = [];
        public List<Badge> SavedBadges { get; } = [];
    }

    private sealed class InMemoryEventReadRepository(EventFlowStore store) : IEventReadRepository
    {
        public Task<IReadOnlyList<EventDto>> GetActiveAsync(CancellationToken ct)
        {
            var result = store.Events.Values
                .Where(e => e.IsActive)
                .Select(MapToDto)
                .ToList();

            return Task.FromResult((IReadOnlyList<EventDto>)result);
        }

        public Task<EventDto?> GetEventByIdAsync(Guid requestEventId, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                store.Events.TryGetValue(requestEventId, out var ev)
                    ? MapToDto(ev)
                    : null);
        }

        private static EventDto MapToDto(Event ev)
            => new(
                ev.Id,
                ev.Name,
                ev.Slug,
                ev.Type.ToString(),
                ev.StartsAtUtc,
                ev.EndsAtUtc,
                ev.DefaultTargetWords,
                ev.IsActive,
                ev.ValidationWindowStartsAtUtc,
                ev.ValidationWindowEndsAtUtc,
                ev.AllowedValidationSources);
    }

    private sealed class InMemoryEventRepository(EventFlowStore store) : IEventRepository
    {
        public Task<List<EventDto>> GetActiveEvents()
        {
            var events = store.Events.Values
                .Where(e => e.IsActive)
                .Select(e => new EventDto(
                    e.Id,
                    e.Name,
                    e.Slug,
                    e.Type.ToString(),
                    e.StartsAtUtc,
                    e.EndsAtUtc,
                    e.DefaultTargetWords,
                    e.IsActive,
                    e.ValidationWindowStartsAtUtc,
                    e.ValidationWindowEndsAtUtc,
                    e.AllowedValidationSources))
                .ToList();

            return Task.FromResult(events);
        }

        public Task<bool> GetEventBySlug(string reqSlug)
            => Task.FromResult(store.Events.Values.Any(e => e.Slug == reqSlug));

        public Task AddEvent(Event ev)
        {
            store.Events[ev.Id] = ev;
            return Task.CompletedTask;
        }

        public Task<Event?> GetEventById(Guid reqEventId)
            => Task.FromResult(store.Events.TryGetValue(reqEventId, out var ev) ? ev : null);

        public Task<List<EventDto>?> GetAllAsync()
            => Task.FromResult<List<EventDto>?>(GetActiveEvents().Result);

        public Task UpdateAsync(Event ev, Guid id)
        {
            store.Events[id] = ev;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Event ev)
        {
            store.Events.Remove(ev.Id);
            return Task.CompletedTask;
        }

        public Task<List<MyEventDto>> GetEventByUserId(Guid userId)
            => Task.FromResult(new List<MyEventDto>());

        public Task<List<EventLeaderboardRowDto>> GetLeaderboard(Event ev, DateTime winStart, DateTime winEnd, int top)
            => Task.FromResult(new List<EventLeaderboardRowDto>());
    }

    private sealed class InMemoryProjectEventsRepository(EventFlowStore store) : IProjectEventsRepository
    {
        public Task<ProjectEvent?> GetByProjectAndEventAsync(Guid projectId, Guid eventId, CancellationToken ct)
        {
            var projectEvent = store.ProjectEvents.Values.FirstOrDefault(x =>
                x.ProjectId == projectId && x.EventId == eventId);

            return Task.FromResult(projectEvent);
        }

        public Task CreateAsync(ProjectEvent entity, CancellationToken ct)
        {
            store.ProjectEvents[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task UpdateTargetWordsAsync(Guid projectEventId, int targetWords, CancellationToken ct)
        {
            if (store.ProjectEvents.TryGetValue(projectEventId, out var projectEvent))
            {
                projectEvent.TargetWords = targetWords;
            }

            return Task.CompletedTask;
        }

        public Task<bool> RemoveByKeysAsync(Guid projectId, Guid eventId, CancellationToken ct)
        {
            var projectEvent = store.ProjectEvents.Values.FirstOrDefault(x =>
                x.ProjectId == projectId && x.EventId == eventId);

            if (projectEvent is null)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(store.ProjectEvents.Remove(projectEvent.Id));
        }

        public Task UpdateProjectEvent(ProjectEvent projectEvent, CancellationToken cancellationToken)
        {
            store.ProjectEvents[projectEvent.Id] = projectEvent;
            return Task.CompletedTask;
        }

        public Task RemoveByKeys(Guid requestProjectId, Guid requestEventId)
            => RemoveByKeysAsync(requestProjectId, requestEventId, CancellationToken.None);
    }

    private sealed class InMemoryProjectEventsReadRepository(EventFlowStore store) : IProjectEventsReadRepository
    {
        public Task<ProjectEvent?> GetByProjectAndEventWithEventAsync(Guid projectId, Guid eventId, CancellationToken ct)
        {
            var projectEvent = store.ProjectEvents.Values.FirstOrDefault(x =>
                x.ProjectId == projectId && x.EventId == eventId);

            AttachEvent(projectEvent);
            return Task.FromResult(projectEvent);
        }

        public Task<ProjectEvent?> GetMostRecentWinByUserIdAsync(Guid userId, CancellationToken ct)
            => Task.FromResult<ProjectEvent?>(null);

        public Task<ProjectEvent?> GetByIdWithEventAsync(Guid projectEventId, CancellationToken ct)
        {
            var projectEvent = store.ProjectEvents.TryGetValue(projectEventId, out var pe) ? pe : null;
            AttachEvent(projectEvent);
            return Task.FromResult(projectEvent);
        }

        public Task<IReadOnlyList<ProjectEvent>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var result = store.ProjectEvents.Values
                .Where(pe => pe.Project?.UserId == userId)
                .ToList();

            foreach (var projectEvent in result)
            {
                AttachEvent(projectEvent);
            }

            return Task.FromResult((IReadOnlyList<ProjectEvent>)result);
        }

        private void AttachEvent(ProjectEvent? projectEvent)
        {
            if (projectEvent is null)
            {
                return;
            }

            if (store.Events.TryGetValue(projectEvent.EventId, out var ev))
            {
                projectEvent.Event = ev;
            }
        }
    }

    private sealed class InMemoryProjectProgressRepository(EventFlowStore store) : IProjectProgressRepository
    {
        public Task<ProjectProgress> AddProgressAsync(ProjectProgress progress, CancellationToken ct)
        {
            store.ProgressEntries.Add(progress);
            return Task.FromResult(progress);
        }

        public Task<bool> DeleteAsync(Guid id, Guid userId) => Task.FromResult(false);

        public Task<IReadOnlyList<ProjectProgress>> GetByProjectAndDateRangeAsync(
            Guid projectId,
            DateTime startUtc,
            DateTime endUtc,
            CancellationToken ct)
        {
            var result = store.ProgressEntries
                .Where(p => p.ProjectId == projectId && p.Date >= startUtc && p.Date <= endUtc)
                .ToList();

            return Task.FromResult((IReadOnlyList<ProjectProgress>)result);
        }
    }

    private sealed class InMemoryBadgeRepository(EventFlowStore store) : IBadgeRepository
    {
        public Task SaveAsync(IEnumerable<Badge> badges)
        {
            store.SavedBadges.AddRange(badges);
            return Task.CompletedTask;
        }
    }
}
