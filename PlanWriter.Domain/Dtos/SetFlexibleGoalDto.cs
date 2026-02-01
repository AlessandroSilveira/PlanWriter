using System;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Dtos;

public class SetFlexibleGoalDto
{
    public int GoalAmount { get; set; }             
    public GoalUnit GoalUnit { get; set; }          
    public DateTime? Deadline { get; set; }        
}