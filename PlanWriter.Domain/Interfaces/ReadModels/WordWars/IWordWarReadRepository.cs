using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.WordWars;

namespace PlanWriter.Domain.Interfaces.ReadModels.WordWars;

public interface IWordWarReadRepository
{
    Task<EventWordWarsDto?> GetByIdAsync(Guid warId, CancellationToken ct = default);
    Task<EventWordWarsDto?> GetActiveByEventIdAsync(Guid eventId, CancellationToken ct = default);
}