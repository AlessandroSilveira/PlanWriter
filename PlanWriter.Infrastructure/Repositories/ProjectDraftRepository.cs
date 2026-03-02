using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class ProjectDraftRepository(IDbExecutor db) : IProjectDraftRepository
{
    public async Task<ProjectDraftDto> UpsertAsync(Guid projectId, Guid userId, string htmlContent, DateTime updatedAtUtc, CancellationToken ct)
    {
        const string updateSql = @"
            UPDATE dbo.ProjectDrafts
            SET
                HtmlContent = @HtmlContent,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE ProjectId = @ProjectId
              AND UserId = @UserId;
        ";

        const string insertSql = @"
            INSERT INTO dbo.ProjectDrafts
            (
                ProjectId,
                UserId,
                HtmlContent,
                CreatedAtUtc,
                UpdatedAtUtc
            )
            VALUES
            (
                @ProjectId,
                @UserId,
                @HtmlContent,
                @UpdatedAtUtc,
                @UpdatedAtUtc
            );
        ";

        const string selectSql = @"
            SELECT
                ProjectId,
                HtmlContent,
                CreatedAtUtc,
                UpdatedAtUtc
            FROM dbo.ProjectDrafts
            WHERE ProjectId = @ProjectId
              AND UserId = @UserId;
        ";

        var affected = await db.ExecuteAsync(updateSql, new
        {
            ProjectId = projectId,
            UserId = userId,
            HtmlContent = htmlContent,
            UpdatedAtUtc = updatedAtUtc
        }, ct);

        if (affected == 0)
        {
            await db.ExecuteAsync(insertSql, new
            {
                ProjectId = projectId,
                UserId = userId,
                HtmlContent = htmlContent,
                UpdatedAtUtc = updatedAtUtc
            }, ct);
        }

        return await db.QueryFirstOrDefaultAsync<ProjectDraftDto>(selectSql, new
        {
            ProjectId = projectId,
            UserId = userId
        }, ct) ?? throw new InvalidOperationException("Project draft could not be persisted.");
    }
}
