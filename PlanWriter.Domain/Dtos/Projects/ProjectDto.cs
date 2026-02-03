using System;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Dtos.Projects;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int CurrentWordCount { get; set; }
    public int? WordCountGoal { get; set; }
    public DateTime? Deadline { get; set; }
    public double ProgressPercent { get; set; }
    public string? Genre { get; set; }
    
    public bool HasCover { get; set; }
    public DateTime? CoverUpdatedAt { get; set; }
    public DateTime StartDate { get; set; }
    public GoalUnit GoalUnit { get; set; } = GoalUnit.Words;
    
}