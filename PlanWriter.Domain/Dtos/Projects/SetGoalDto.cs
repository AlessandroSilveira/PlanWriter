using System;

namespace PlanWriter.Domain.Dtos.Projects;

public class SetGoalDto
{
    public int WordCountGoal { get; set; }
    public DateTime? Deadline { get; set; }
}