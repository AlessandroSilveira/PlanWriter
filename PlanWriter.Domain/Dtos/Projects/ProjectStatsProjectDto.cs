using System;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Dtos.Projects;

public record ProjectStatsProjectDto(
    Guid Id,
    DateTime StartDate,
    DateTime? Deadline,
    int? WordCountGoal,
    int GoalAmount,
    GoalUnit GoalUnit
);