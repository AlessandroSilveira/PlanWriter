using System;

namespace PlanWriter.Domain.Dtos.Projects;

public class ProjectDraftDto
{
    public Guid ProjectId { get; set; }
    public string HtmlContent { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
