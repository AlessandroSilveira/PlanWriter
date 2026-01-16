using System;

namespace PlanWriter.Domain.Dtos.Buddies;

public class BuddyLeaderboardRowDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public int Total { get; set; }
    public int PaceDelta { get; set; }
    public bool IsMe { get; set; }
}