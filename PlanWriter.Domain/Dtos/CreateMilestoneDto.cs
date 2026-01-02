using System;

namespace PlanWriter.Domain.Dtos;

public class CreateMilestoneDto
{
    public string? Name { get; set; }

    /// <summary>
    /// Valor alvo (ex: 10000 palavras)
    /// </summary>
    public int TargetAmount { get; set; }

    /// <summary>
    /// Ordem opcional para exibição
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Data limite opcional
    /// </summary>
    public DateTime? DueDate { get; set; }

    public string? Notes { get; set; }
}