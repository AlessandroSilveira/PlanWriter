using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Badges.Dtos.Queries;
using PlanWriter.Domain.Dtos.Badges;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Badges;

namespace PlanWriter.Application.Badges.Queries;

public class GetBadgesByProjectIdQueryHandler(IBadgeReadRepository badgeReadRepository, ILogger<GetBadgesByProjectIdQueryHandler> logger
)
    : IRequestHandler<GetBadgesByProjectIdQuery, IReadOnlyList<BadgeDto>>
{
    public async Task<IReadOnlyList<BadgeDto>> Handle(GetBadgesByProjectIdQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting badges for project {ProjectId} user {UserId}", request.ProjectId, request.UserId);

        var badges = await badgeReadRepository.GetByProjectIdAsync(request.ProjectId, request.UserId, ct);

        logger.LogInformation("Found {Count} badges for project {ProjectId}", badges.Count, request.ProjectId);

        var listBadgeDto = new List<BadgeDto>();

        return MapBadgesToBadgeDtos(badges, listBadgeDto);
    }

    private static IReadOnlyList<BadgeDto> MapBadgesToBadgeDtos(IReadOnlyList<Badge> badges, List<BadgeDto> listBadgeDto)
    {
        foreach (var badge in badges)
        {
            var badgeDto = new BadgeDto()
            {
                AwardedAt = badge.AwardedAt,
                Name = badge.Name,
                Id = badge.Id,
                Description = badge.Description,
                EventId = badge.EventId,
                ProjectId = badge.ProjectId,
                Icon = badge.Icon

            };
            listBadgeDto.Add(badgeDto);
        }

        return listBadgeDto;
    }
}