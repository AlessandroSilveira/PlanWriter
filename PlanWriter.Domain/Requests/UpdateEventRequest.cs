using System;

namespace PlanWriter.Domain.Requests;

public class UpdateEventRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TargetWords { get; set; }
    public bool IsActive { get; set; }
}