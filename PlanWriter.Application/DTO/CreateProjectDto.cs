using System;

namespace PlanWriter.Application.DTO
{
    public class CreateProjectDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int? WordCountGoal { get; set; } // Meta opcional
        public DateTime? Deadline { get; set; } // Prazo opcional
    }
}