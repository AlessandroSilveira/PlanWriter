using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.WordWars;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.WordWars;

public class WordWarParticipantReadRepository(IDbExecutor db) : IWordWarParticipantReadRepository
{
    public Task<IReadOnlyList<EventWordWarParticipantsDto>> GetScoreboardAsync(Guid warId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                p.Id,
                p.WordWarId,
                p.UserId,
                p.ProjectId,
                p.JoinedAtUtc,
                p.WordsInRound,
                p.LastCheckpointAtUtc,
                ROW_NUMBER() OVER (
                    ORDER BY p.WordsInRound DESC,
                             p.LastCheckpointAtUtc ASC,
                             p.JoinedAtUtc ASC
                ) AS FinalRank
            FROM EventWordWarParticipants p
            WHERE p.WordWarId = @WarId;";

        return db.QueryAsync<EventWordWarParticipantsDto>(sql, new { WarId = warId }, ct);
    }

    public Task<IReadOnlyList<EventWordWarParticipantsDto>?> GetAllParticipant(Guid warId, CancellationToken ct)
    {
        const string sql = @"
                       SELECT TOP 1
                p.Id,
                p.WordWarId,
                p.UserId,
                p.ProjectId,
                p.JoinedAtUtc,
                p.WordsInRound,
                p.LastCheckpointAtUtc,
                p.FinalRank
            FROM EventWordWarParticipants p
            WHERE p.WordWarId = @WarId;              
            ";
        
        return db.QueryAsync<EventWordWarParticipantsDto>(sql, new { WarId = warId  }, ct);
    }

    public Task<EventWordWarParticipantsDto?> GetParticipant(Guid warId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
                       SELECT TOP 1
                p.Id,
                p.WordWarId,
                p.UserId,
                p.ProjectId,
                p.JoinedAtUtc,
                p.WordsInRound,
                p.LastCheckpointAtUtc,
                p.FinalRank
            FROM EventWordWarParticipants p
            WHERE p.WordWarId = @WarId
              AND p.UserId = @UserId;
            ";

        return db.QueryFirstOrDefaultAsync<EventWordWarParticipantsDto>(sql, new { WarId = warId, UserId = userId  }, ct);
    }
}