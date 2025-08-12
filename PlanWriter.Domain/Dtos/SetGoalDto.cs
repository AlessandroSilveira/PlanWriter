using System;

namespace PlanWriter.Domain.Dtos;

public class SetGoalDto
{
    public int WordCountGoal { get; set; }
    public DateTime? Deadline { get; set; }
}