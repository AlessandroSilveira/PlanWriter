using System;

namespace PlanWriter.Application.DTO
{
    public class AddProjectProgressDto
    {
        public Guid ProjectId { get; set; }
        public int WordsWritten { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}