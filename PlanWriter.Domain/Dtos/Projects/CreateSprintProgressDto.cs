using System;

namespace PlanWriter.Domain.Dtos.Projects;

public class CreateSprintProgressDto
{
    public Guid ProjectId { get; set; }
    public int Words { get; set; }
    public int Minutes { get; set; }
    public DateTime Date { get; set; }
}
