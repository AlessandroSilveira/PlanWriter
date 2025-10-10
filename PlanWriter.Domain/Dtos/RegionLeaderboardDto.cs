using System;

namespace PlanWriter.Domain.Dtos;


public record RegionLeaderboardDto(Guid RegionId, string Region, int TotalWords, int UserCount);