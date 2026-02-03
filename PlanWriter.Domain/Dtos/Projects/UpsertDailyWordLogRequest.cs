using System;

namespace PlanWriter.Domain.Dtos.Projects;

public class UpsertDailyWordLogRequest
{
    public Guid ProjectId { get; set; }
    public DateOnly Date { get; set; }
    public int WordsWritten { get; set; }
}