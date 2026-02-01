using System;

namespace PlanWriter.Domain.Requests;

public record UpdateMyProfileRequest(
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    bool? IsProfilePublic,
    string? Slug,
    Guid[]? PublicProjectIds
);