// Contracts/ProfileDtos.cs

using System;

namespace PlanWriter.Application.DTO
{
    public record MyProfileDto(
        string Email,
        string? DisplayName,
        string? Bio,
        string? AvatarUrl,
        bool IsProfilePublic,
        string? Slug,
        Guid[] PublicProjectIds
    );
}