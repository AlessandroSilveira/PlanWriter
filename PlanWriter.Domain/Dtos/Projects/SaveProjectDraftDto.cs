using System;

namespace PlanWriter.Domain.Dtos.Projects;

public class SaveProjectDraftDto
{
    public string HtmlContent { get; set; } = string.Empty;
    public DateTime? LastKnownUpdatedAtUtc { get; set; }
}
