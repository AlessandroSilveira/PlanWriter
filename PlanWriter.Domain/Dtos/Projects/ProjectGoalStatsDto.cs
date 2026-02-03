using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Dtos.Projects;

public record ProjectGoalStatsDto(int GoalAmount, GoalUnit Unit, int Accumulated, int Remaining);