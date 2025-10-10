using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Dtos;

public record ProjectGoalStatsDto(int GoalAmount, GoalUnit Unit, int Accumulated, int Remaining);