using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Profile.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Profile.Queries;

public class GetMineProfileQueryHandler(
    ILogger<GetMineProfileQueryHandler> logger,
    IUserReadRepository userReadRepository,
    IProjectRepository projectRepository)
    : IRequestHandler<GetMineProfileQuery, MyProfileDto>
{
    public async Task<MyProfileDto> Handle(GetMineProfileQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting profile for user {UserId}", request.UserId);

        var user = await userReadRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        var allProjects = await projectRepository.GetAllAsync(cancellationToken);

        var publicProjectIds = allProjects
            .Where(project =>
                project.UserId == request.UserId &&
                project.IsPublic)
            .Select(project => project.Id)
            .ToArray();

        logger.LogInformation("User {UserId} has {Count} public projects", request.UserId, publicProjectIds.Length);

        return new MyProfileDto(
            Email: user.Email!,
            DisplayName: user.DisplayName,
            Bio: user.Bio,
            AvatarUrl: user.AvatarUrl,
            IsProfilePublic: user.IsProfilePublic,
            Slug: user.Slug,
            PublicProjectIds: publicProjectIds
        );
    }
}
