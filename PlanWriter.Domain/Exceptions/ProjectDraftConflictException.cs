using System;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Domain.Exceptions;

public sealed class ProjectDraftConflictException(ProjectDraftDto currentDraft)
    : Exception("Project draft is out of date.")
{
    public ProjectDraftDto CurrentDraft { get; } = currentDraft;
}
