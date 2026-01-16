using System;

namespace PlanWriter.Domain.Entities;

public class DailyWordLog
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }

    public DateOnly Date { get; set; }

    public int WordsWritten { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
   
}