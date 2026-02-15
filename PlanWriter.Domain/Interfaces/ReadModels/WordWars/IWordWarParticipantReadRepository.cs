using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.WordWars;

namespace PlanWriter.Domain.Interfaces.ReadModels.WordWars;

public interface IWordWarParticipantReadRepository
{
    Task<IReadOnlyList<EventWordWarParticipantsDto>> GetScoreboardAsync(Guid warId, CancellationToken ct = default);
}