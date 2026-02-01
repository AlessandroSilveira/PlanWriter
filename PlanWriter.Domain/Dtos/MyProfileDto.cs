// Contracts/ProfileDtos.cs

using System;

namespace PlanWriter.Domain.Dtos
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