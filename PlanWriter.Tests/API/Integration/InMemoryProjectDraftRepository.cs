using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Exceptions;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Tests.API.Integration;

public sealed class InMemoryProjectDraftRepository : IProjectDraftRepository, IProjectDraftReadRepository
{
    private sealed record StoredDraft(Guid ProjectId, Guid UserId, string HtmlContent, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

    private readonly ConcurrentDictionary<Guid, StoredDraft> _drafts = new();

    public Task<ProjectDraftDto?> GetByProjectIdAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        if (!_drafts.TryGetValue(projectId, out var draft) || draft.UserId != userId)
            return Task.FromResult<ProjectDraftDto?>(null);

        return Task.FromResult<ProjectDraftDto?>(new ProjectDraftDto
        {
            ProjectId = draft.ProjectId,
            HtmlContent = draft.HtmlContent,
            CreatedAtUtc = draft.CreatedAtUtc,
            UpdatedAtUtc = draft.UpdatedAtUtc
        });
    }

    public Task<ProjectDraftDto> UpsertAsync(
        Guid projectId,
        Guid userId,
        string htmlContent,
        DateTime updatedAtUtc,
        DateTime? lastKnownUpdatedAtUtc,
        CancellationToken ct)
    {
        if (_drafts.TryGetValue(projectId, out var existingDraft) && existingDraft.UserId == userId)
        {
            if (lastKnownUpdatedAtUtc.HasValue && existingDraft.UpdatedAtUtc != lastKnownUpdatedAtUtc.Value)
                throw new ProjectDraftConflictException(new ProjectDraftDto
                {
                    ProjectId = existingDraft.ProjectId,
                    HtmlContent = existingDraft.HtmlContent,
                    CreatedAtUtc = existingDraft.CreatedAtUtc,
                    UpdatedAtUtc = existingDraft.UpdatedAtUtc
                });
        }

        var stored = _drafts.AddOrUpdate(
            projectId,
            _ => new StoredDraft(projectId, userId, htmlContent, updatedAtUtc, updatedAtUtc),
            (_, existing) => existing.UserId == userId
                ? existing with { HtmlContent = htmlContent, UpdatedAtUtc = updatedAtUtc }
                : new StoredDraft(projectId, userId, htmlContent, updatedAtUtc, updatedAtUtc));

        return Task.FromResult(new ProjectDraftDto
        {
            ProjectId = stored.ProjectId,
            HtmlContent = stored.HtmlContent,
            CreatedAtUtc = stored.CreatedAtUtc,
            UpdatedAtUtc = stored.UpdatedAtUtc
        });
    }

    public void Reset() => _drafts.Clear();
}
