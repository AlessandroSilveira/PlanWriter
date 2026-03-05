using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
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

        static DynamicParameters CreateDraftWriteParameters(
            Guid projectId,
            Guid userId,
            string htmlContent,
            DateTime updatedAtUtc,
            DateTime? expectedUpdatedAtUtc = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("ProjectId", projectId, DbType.Guid);
            parameters.Add("UserId", userId, DbType.Guid);
            parameters.Add("HtmlContent", htmlContent, DbType.String);
            parameters.Add("UpdatedAtUtc", updatedAtUtc, DbType.DateTime2);
            if (expectedUpdatedAtUtc.HasValue)
                parameters.Add("ExpectedUpdatedAtUtc", expectedUpdatedAtUtc.Value, DbType.DateTime2);
            return parameters;
        }

        if (currentDraft is null)
        {
            var insertParameters = CreateDraftWriteParameters(
                projectId,
                userId,
                htmlContent,
                updatedAtUtc);

            await db.ExecuteAsync(insertSql, insertParameters, ct);
        }
        else
        {
            if (lastKnownUpdatedAtUtc.HasValue && currentDraft.UpdatedAtUtc != lastKnownUpdatedAtUtc.Value)
                throw new ProjectDraftConflictException(currentDraft);

            var updateParameters = CreateDraftWriteParameters(
                projectId,
                userId,
                htmlContent,
                updatedAtUtc,
                currentDraft.UpdatedAtUtc);

            var affected = await db.ExecuteAsync(updateSql, updateParameters, ct);

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
