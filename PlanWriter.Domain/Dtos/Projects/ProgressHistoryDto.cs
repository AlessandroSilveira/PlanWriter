using System;

namespace PlanWriter.Domain.Dtos.Projects;

public class ProgressHistoryDto
{
    public DateTime Date { get; set; }
    public int WordsWritten { get; set; }
}