using System;

namespace PlanWriter.Domain.Dtos;

public class AddProjectProgressDto
{
    public Guid ProjectId { get; set; }

    // Agora todos opcionais — o Service valida conforme a meta do projeto
    public int? WordsWritten { get; set; }
    public int? Minutes { get; set; }
    public int? Pages { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}