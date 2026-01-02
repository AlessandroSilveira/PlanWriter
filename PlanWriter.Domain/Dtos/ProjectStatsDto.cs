using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Dtos;

public class ProjectStatsDto
{
    public int TotalWords { get; set; }
    public int AveragePerDay { get; set; }
    public BestDayDto? BestDay { get; set; }
    public int ActiveDays { get; set; }
    public int? WordsRemaining { get; set; }
    public int? SmartDailyTarget { get; set; }
    
    public string MotivationMessage { get; set; }
    
    public ProjectStatus Status { get; set; }
    public string? StatusReason { get; set; }
    public int TargetPerDay { get; set; }
}

public class BestDayDto
{
    public string Date { get; set; } = "";
    public int Words { get; set; }
}
