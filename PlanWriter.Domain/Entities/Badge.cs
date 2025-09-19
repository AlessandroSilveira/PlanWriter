using System;

namespace PlanWriter.Domain.Entities;

public class Badge
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Icon { get; set; } = null!; // opcional: emoji ou nome do ícone
    public DateTime AwardedAt { get; set; }

    public Guid ProjectId { get; set; }
    public Guid? EventId { get; set; }
    
}
