using System;

namespace PlanWriter.Domain.Dtos.Projects;

public class CreateProjectDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Genre { get; set; }
    public int? WordCountGoal { get; set; } // Meta opcional
    public DateTime? Deadline { get; set; } // Prazo opcional
    public DateTime? StartDate { get; set; } 
}