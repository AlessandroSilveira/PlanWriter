namespace PlanWriter.Domain.Dtos;

public record PublicProfileDto(
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string Slug,
    PublicProjectSummaryDto[] Projects,
    string? Highlight            // ex.: “Winner — NaNoWriMo 2025”
);