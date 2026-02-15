using System;

namespace PlanWriter.Domain.Dtos.WordWars;

public class EventWordWarParticipantsDto
{
    public Guid Id { get; set; }
    public Guid WordWarId { get; set; }
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime JoinedAtUtc { get; set; }
    public int WordsInRound { get; set; }
    public DateTime LastCheckpointAtUtc { get; set; }
    public int FinalRank { get; set; }
}