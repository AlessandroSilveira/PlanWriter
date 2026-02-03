using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Certificates;
using PlanWriter.Domain.Interfaces.ReadModels.Certificates;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Certificates;

public sealed class CertificateReadRepository(IDbExecutor db) : ICertificateReadRepository
{
    public Task<CertificateWinnerRow?> GetWinnerRowAsync(Guid projectId, Guid eventId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT TOP 1
                e.Name AS EventName,
                pe.ValidatedAtUtc,
                pe.Won,
                COALESCE(pe.FinalWordCount, 0) AS FinalWordCount,
                COALESCE(p.Title, 'Projeto') AS ProjectTitle
            FROM ProjectEvents pe
            INNER JOIN Projects p ON p.Id = pe.ProjectId
            INNER JOIN Events e ON e.Id = pe.EventId
            WHERE pe.ProjectId = @ProjectId
              AND pe.EventId = @EventId
              AND p.UserId = @UserId;
        ";

        return db.QueryFirstOrDefaultAsync<CertificateWinnerRow>(
            sql,
            new { ProjectId = projectId, EventId = eventId, UserId = userId },
            ct
        );
    }
}