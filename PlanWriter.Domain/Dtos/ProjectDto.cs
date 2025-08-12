using System;

namespace PlanWriter.Domain.Dtos;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int CurrentWordCount { get; set; }
    public int? WordCountGoal { get; set; }
    public DateTime? Deadline { get; set; }
    public double ProgressPercent { get; set; }
}