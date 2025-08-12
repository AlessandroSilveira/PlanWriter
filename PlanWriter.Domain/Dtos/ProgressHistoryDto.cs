using System;

namespace PlanWriter.Domain.Dtos;

public class ProgressHistoryDto
{
    public DateTime Date { get; set; }
    public int WordsWritten { get; set; }
}