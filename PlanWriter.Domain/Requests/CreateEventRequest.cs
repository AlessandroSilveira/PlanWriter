using System;

public class CreateEventRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TargetWords { get; set; }
    public string Slug { get; set; } = null!;
    public string Type { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int? DefaultTargetWords { get; set; }
}