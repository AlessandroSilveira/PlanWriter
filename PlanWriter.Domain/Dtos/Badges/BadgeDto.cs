using System;

namespace PlanWriter.Domain.Dtos.Badges;

public class BadgeDto
{
    public int Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? EventId { get; set; }

    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Icon { get; set; } = default!;
    public DateTime AwardedAt { get; set; }
}
