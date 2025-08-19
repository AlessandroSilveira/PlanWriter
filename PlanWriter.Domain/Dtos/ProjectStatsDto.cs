namespace PlanWriter.Domain.Dtos;

public class ProjectStatsDto
{
    public int TotalWords { get; set; }
    public int AveragePerDay { get; set; }
    public BestDayDto? BestDay { get; set; }
    public int ActiveDays { get; set; }
    public int? WordsRemaining { get; set; }
    public int? SmartDailyTarget { get; set; }
}

public class BestDayDto
{
    public string Date { get; set; } = "";
    public int Words { get; set; }
}
