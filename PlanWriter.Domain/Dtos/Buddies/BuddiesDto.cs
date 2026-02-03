using System;

namespace PlanWriter.Domain.Dtos.Buddies;

public class BuddiesDto
{
    public record FollowBuddyByUsernameRequest(string Email);
    public record FollowBuddyByIdRequest(Guid FolloweeId);

    public record BuddySummaryDto(Guid UserId, string Username, string DisplayName, string? AvatarUrl);

    public record BuddyLeaderboardItemDto(
        Guid UserId,
        string Username,
        string DisplayName,
        int Total,
        int? PaceDelta 
    );

}