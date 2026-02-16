using System;
using System.Collections.Generic;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Dtos.WordWars;

public class WordWarScoreboardDto
{
    public Guid Id { get; set; }
    public WordWarStatus Status { get; set; }
    public int DurationMinutes { get; set; }
    public int RemainingSeconds { get; set; }
    public int RemainingSecnds { get; set; }
    public List<EventWordWarParticipantsDto> Participants { get; set; } = new();
}
