using System;

namespace PlanWriter.Domain.Dtos;

public class AddProjectProgressDto
{
    public Guid ProjectId { get; set; }
    public int WordsWritten { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}