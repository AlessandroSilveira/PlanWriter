using System;

namespace PlanWriter.Domain.Dtos;

public class ValidateTextDto
{
    public Guid ProjectId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Notes { get; set; }
}