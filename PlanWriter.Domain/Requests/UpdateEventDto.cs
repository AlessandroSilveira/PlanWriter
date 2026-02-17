using System;

namespace PlanWriter.Domain.Requests;

public class UpdateEventDto
{
    public bool IsActive { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int TargetWords { get; set; }
    public DateTime? ValidationWindowStartsAtUtc { get; set; }
    public DateTime? ValidationWindowEndsAtUtc { get; set; }
    public string? AllowedValidationSources { get; set; }
   
}
