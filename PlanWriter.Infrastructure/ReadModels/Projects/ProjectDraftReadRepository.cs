using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.ReadModels.Projects;

public class ProjectDraftReadRepository(IDbExecutor db) : IProjectDraftReadRepository
{
    public Task<ProjectDraftDto?> GetByProjectIdAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                ProjectId,
                HtmlContent,
                CreatedAtUtc,
                UpdatedAtUtc
            FROM dbo.ProjectDrafts
            WHERE ProjectId = @ProjectId
              AND UserId = @UserId;
        ";

        return db.QueryFirstOrDefaultAsync<ProjectDraftDto>(sql, new
        {
            ProjectId = projectId,
            UserId = userId
        }, ct);
    }
}
