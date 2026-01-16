using System;

namespace PlanWriter.Domain.Dtos;

public class BuddiesDto
{
    // PlanWriter.Application/DTOs/BuddiesDtos.cs
    public record FollowBuddyByUsernameRequest(string Email);
    public record FollowBuddyByIdRequest(Guid FolloweeId);

    public record BuddySummaryDto(Guid UserId, string Username, string DisplayName, string? AvatarUrl);

    public record BuddyLeaderboardItemDto(
        Guid UserId,
        string Username,
        string DisplayName,
        int Total,
        int? PaceDelta // diferença vs. você
    );

}