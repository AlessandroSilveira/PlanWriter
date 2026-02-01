using System;

namespace PlanWriter.Domain.Dtos.Projects;

public record ProjectProgressDayDto(
    DateTime Date,
    int TotalWords,
    int TotalMinutes,
    int TotalPages
);