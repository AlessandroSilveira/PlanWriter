using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common;
using PlanWriter.Application.Profile.Dtos.Commands;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Requests;

namespace PlanWriter.Application.Profile.Commands;

public class UpdateProfileCommandHandler(
    IUserReadRepository userReadRepository,
    IUserRepository userRepository,
    IProjectRepository projectRepository,
    ILogger<UpdateProfileCommandHandler> logger, 
    IProjectReadRepository projectReadRepository) : IRequestHandler<UpdateProfileCommand, MyProfileDto>
{
    public async Task<MyProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken) 
    {
        logger.LogInformation("Updating profile for user {UserId}", request.UserId);

        var user = await userReadRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        ApplyProfileChanges(user, request.Request);

        await EnsureSlugIsValidAsync(user, request.Request, cancellationToken);

        await UpdatePublicProjectsAsync(request.UserId, request.Request, cancellationToken);

        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("Profile updated for user {UserId}", request.UserId);

        return MapToProfileDto(user, await projectReadRepository.GetUserProjectsAsync(user.Id, cancellationToken));
    }

    /* ===================== PRIVATE METHODS ===================== */

    private static void ApplyProfileChanges(User user, UpdateMyProfileRequest request)
    {
        if (request.DisplayName != null)
            user.DisplayName = request.DisplayName.Trim();

        if (request.Bio != null)
            user.Bio = request.Bio.Trim();

        if (request.AvatarUrl != null)
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl)
                ? null
                : request.AvatarUrl.Trim();

        if (request.IsProfilePublic.HasValue)
            user.IsProfilePublic = request.IsProfilePublic.Value;
    }

    private async Task EnsureSlugIsValidAsync(User user, UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Slug != null)
        {
            var slug = Slugify.From(request.Slug);

            if (string.IsNullOrWhiteSpace(slug))
                throw new InvalidOperationException("Slug inválido.");

            if (await userReadRepository.SlugExistsAsync(slug, user.Id, cancellationToken))
                throw new InvalidOperationException("Este slug já está em uso.");

            user.Slug = slug;
        }
        else if (string.IsNullOrWhiteSpace(user.Slug) &&
                 !string.IsNullOrWhiteSpace(user.DisplayName))
        {
            var baseSlug = Slugify.From(user.DisplayName);
            var slug = baseSlug;
            var i = 2;

            while (await userReadRepository.SlugExistsAsync(slug, user.Id, cancellationToken))
                slug = $"{baseSlug}-{i++}";

            user.Slug = slug;
        }
    }

    private async Task UpdatePublicProjectsAsync(Guid userId, UpdateMyProfileRequest request, CancellationToken ct)
    {
        if (request.PublicProjectIds is null)
            return;

        var publicIds = request.PublicProjectIds
            .Distinct()
            .ToHashSet();

        // READ → DTO
        var projects = await projectReadRepository
            .GetUserProjectsAsync(userId, ct);

        foreach (var dto in projects)
        {
            var isPublic = publicIds.Contains(dto.Id);
            await projectRepository.SetProjectVisibilityAsync(dto.Id, userId, isPublic, ct);
        }
    }


    private static MyProfileDto MapToProfileDto(User user, IReadOnlyList<ProjectDto> projects)
    {
        return new MyProfileDto(
            Email: user.Email!,
            DisplayName: user.DisplayName,
            Bio: user.Bio,
            AvatarUrl: user.AvatarUrl,
            IsProfilePublic: user.IsProfilePublic,
            Slug: user.Slug,
            PublicProjectIds: projects
                .Where(p => p.IsPublic)
                .Select(p => p.Id)
                .ToArray()
        );
    }
}
