using System;

namespace PlanWriter.Domain.Dtos;

public class ProjectStatisticsDto
{
    public int TotalWordsWritten { get; set; }
    public double AverageWordsPerDay { get; set; }
    public DateTime? MostProductiveDay { get; set; }
    public double? CompletionPercentage { get; set; }
}