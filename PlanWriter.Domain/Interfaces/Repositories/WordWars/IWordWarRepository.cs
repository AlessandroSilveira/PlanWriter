using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Interfaces.Repositories.WordWars;

public interface IWordWarRepository
{
    Task<Guid> CreateAsync(
        Guid eventId,
        Guid createdByUserId,
        int durationMinutes,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        WordWarStatus status,
        CancellationToken ct = default);

    Task<int> StartAsync(Guid warId, DateTime startsAtUtc, DateTime endsAtUtc, CancellationToken ct = default);
    Task<int> FinishAsync(Guid warId, DateTime finishedAtUtc, CancellationToken ct = default);

    Task<int> JoinAsync(Guid warId, Guid userId, Guid projectId, CancellationToken ct = default);
    Task<int> LeaveAsync(Guid warId, Guid userId, CancellationToken ct = default);

    Task<int> SubmitCheckpointAsync(
        Guid warId,
        Guid userId,
        int wordsInRound,
        DateTime checkpointAtUtc,
        CancellationToken ct = default);
    
    // /Users/alessandrosilveira/Documents/Repos/PlanWriter/PlanWriter.Domain/Interfaces/Repositories/WordWars/IWordWarRepository.cs
    Task<int> PersistFinalRankAsync(Guid warId, CancellationToken ct = default);

}