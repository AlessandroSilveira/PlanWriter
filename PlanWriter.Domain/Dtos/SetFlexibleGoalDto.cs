// PlanWriter.Domain/Dtos/Projects/SetFlexibleGoalDto.cs

using System;
using PlanWriter.Domain.Enums; // GoalUnit

public class SetFlexibleGoalDto
{
    public int GoalAmount { get; set; }             // ex.: 50000, 3000, 200
    public GoalUnit GoalUnit { get; set; }          // Words | Minutes | Pages
    public DateTime? Deadline { get; set; }         // opcional (mantém seu campo)
}