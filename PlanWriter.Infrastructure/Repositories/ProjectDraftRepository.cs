using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Exceptions;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class ProjectDraftRepository(IDbExecutor db) : IProjectDraftRepository
{
    public async Task<ProjectDraftDto> UpsertAsync(
        Guid projectId,
        Guid userId,
        string htmlContent,
        DateTime updatedAtUtc,
        DateTime? lastKnownUpdatedAtUtc,
        CancellationToken ct)
    {
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

        const string updateSql = @"
            UPDATE dbo.ProjectDrafts
            SET
                HtmlContent = @HtmlContent,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE ProjectId = @ProjectId
              AND UserId = @UserId
              AND UpdatedAtUtc = @ExpectedUpdatedAtUtc;
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

        var currentDraft = await db.QueryFirstOrDefaultAsync<ProjectDraftDto>(selectSql, new
        {
            ProjectId = projectId,
            UserId = userId
        }, ct);

        if (currentDraft is null)
        {
            await db.ExecuteAsync(insertSql, new
            {
                ProjectId = projectId,
                UserId = userId,
                HtmlContent = htmlContent,
                UpdatedAtUtc = updatedAtUtc
            }, ct);
        }
        else
        {
            if (lastKnownUpdatedAtUtc.HasValue && currentDraft.UpdatedAtUtc != lastKnownUpdatedAtUtc.Value)
                throw new ProjectDraftConflictException(currentDraft);

            var affected = await db.ExecuteAsync(updateSql, new
            {
                ProjectId = projectId,
                UserId = userId,
                HtmlContent = htmlContent,
                UpdatedAtUtc = updatedAtUtc,
                ExpectedUpdatedAtUtc = currentDraft.UpdatedAtUtc
            }, ct);

            if (affected == 0)
            {
                var latestDraft = await db.QueryFirstOrDefaultAsync<ProjectDraftDto>(selectSql, new
                {
                    ProjectId = projectId,
                    UserId = userId
                }, ct);

                if (latestDraft is not null)
                    throw new ProjectDraftConflictException(latestDraft);
            }
        }

        return await db.QueryFirstOrDefaultAsync<ProjectDraftDto>(selectSql, new
        {
            ProjectId = projectId,
            UserId = userId
        }, ct) ?? throw new InvalidOperationException("Project draft could not be persisted.");
    }
}
