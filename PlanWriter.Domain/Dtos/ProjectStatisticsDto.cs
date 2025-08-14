using System;

namespace PlanWriter.Domain.Dtos;

public class ProjectStatisticsDto
{
    public int TotalWordsWritten { get; set; }
    public int? WordCountGoal { get; set; }
    public int RemainingWords { get; set; }
    public double ProgressPercentage { get; set; }
    public double AverageWordsPerDay { get; set; }
    public int TotalTimeSpentInMinutes { get; set; }
    public DateTime? Deadline { get; set; }
    public int DaysRemaining { get; set; }
}