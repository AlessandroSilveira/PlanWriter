using System;

namespace PlanWriter.Application.DTO;

public record UpdateMyProfileRequest(
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    bool? IsProfilePublic,
    string? Slug,
    Guid[]? PublicProjectIds
);