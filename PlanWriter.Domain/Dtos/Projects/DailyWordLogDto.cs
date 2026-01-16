using System;

namespace PlanWriter.Domain.Dtos.Projects;

public class DailyWordLogDto
{
    public DateOnly Date { get; set; }
    public int WordsWritten { get; set; }
}