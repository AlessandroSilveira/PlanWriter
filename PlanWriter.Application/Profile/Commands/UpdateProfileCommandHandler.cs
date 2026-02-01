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
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Requests;

namespace PlanWriter.Application.Profile.Commands;

public class UpdateProfileCommandHandler(IUserRepository userRepository, IProjectRepository projectRepository,
    ILogger<UpdateProfileCommandHandler> logger) : IRequestHandler<UpdateProfileCommand, MyProfileDto>
{
    public async Task<MyProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken) 
    {
        logger.LogInformation("Updating profile for user {UserId}", request.UserId);

        var user = await userRepository.GetByIdAsync(request.UserId)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        ApplyProfileChanges(user, request.Request);

        await EnsureSlugIsValidAsync(user, request.Request);

        await UpdatePublicProjectsAsync(request.UserId, request.Request);

        await userRepository.UpdateAsync(user);

        logger.LogInformation(
            "Profile updated for user {UserId}",
            request.UserId
        );

        return MapToProfileDto(user, await projectRepository.GetByUserIdAsync(user.Id));
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

    private async Task EnsureSlugIsValidAsync(User user, UpdateMyProfileRequest request)
    {
        if (request.Slug != null)
        {
            var slug = Slugify.From(request.Slug);

            if (string.IsNullOrWhiteSpace(slug))
                throw new InvalidOperationException("Slug inválido.");

            if (await userRepository.SlugExistsAsync(slug, user.Id))
                throw new InvalidOperationException("Este slug já está em uso.");

            user.Slug = slug;
        }
        else if (string.IsNullOrWhiteSpace(user.Slug) &&
                 !string.IsNullOrWhiteSpace(user.DisplayName))
        {
            var baseSlug = Slugify.From(user.DisplayName);
            var slug = baseSlug;
            var i = 2;

            while (await userRepository.SlugExistsAsync(slug, user.Id))
                slug = $"{baseSlug}-{i++}";

            user.Slug = slug;
        }
    }

    private async Task UpdatePublicProjectsAsync(Guid userId, UpdateMyProfileRequest request)
    {
        if (request.PublicProjectIds is null)
            return;

        var publicIds = request.PublicProjectIds.Distinct().ToHashSet();
        var projects = await projectRepository.GetByUserIdAsync(userId);

        foreach (var project in projects)
        {
            project.IsPublic = publicIds.Contains(project.Id);
            await projectRepository.UpdateAsync(project);
        }
    }

    private static MyProfileDto MapToProfileDto(User user, List<Project> projects)
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
