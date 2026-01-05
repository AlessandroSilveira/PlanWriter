using System;
using System.Collections.Generic;

namespace PlanWriter.Domain.Events;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public EventType Type  { get; set; } = EventType.Nanowrimo!;
    public DateTime StartsAtUtc { get; set; }    // sempre UTC
    public DateTime EndsAtUtc   { get; set; }

    // Meta padrão (pode ser sobrescrita pelo participante)
    public int? DefaultTargetWords { get; set; }

    public bool IsActive { get; set; } = true;

    // Navegação
    public ICollection<ProjectEvent> Participants { get; set; } = new List<ProjectEvent>();
}