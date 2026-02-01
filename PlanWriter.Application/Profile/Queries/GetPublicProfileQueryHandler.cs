using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Profile.Dtos.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Profile.Queries;

public class GetPublicProfileQueryHandler(IUserRepository userRepository, IEventRepository eventRepository,
    IProjectRepository projectRepository, IProjectEventsRepository projectEventsRepository,
    IProjectProgressRepository projectProgressRepository, ILogger<GetPublicProfileQueryHandler> logger)
    : IRequestHandler<GetPublicProfileQuery, PublicProfileDto>
{
    public async Task<PublicProfileDto> Handle(GetPublicProfileQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Getting public profile for slug {Slug}",
            request.Slug
        );

        // 1️⃣ Usuário
        var user = await userRepository.GetBySlugAsync(request.Slug)
            ?? throw new KeyNotFoundException("Perfil não encontrado.");

        if (!user.IsProfilePublic)
            throw new InvalidOperationException("Perfil não é público.");

        // 2️⃣ Evento global ativo (se existir)
        var now = DateTime.UtcNow;
        var activeEventList = await eventRepository.GetActiveEvents();
        var activeEvent = activeEventList.FirstOrDefault(a => a.EndsAtUtc > now);

        // 3️⃣ Projetos públicos
        var publicProjects = await projectRepository.GetPublicProjectsByUserIdAsync(user.Id);

        var projectSummaries = new List<PublicProjectSummaryDto>();

        foreach (var project in publicProjects)
        {
            var summary = await BuildProjectSummaryAsync(
                project,
                activeEvent,
                user.Id
            );

            projectSummaries.Add(summary);
        }
        
        var highlight = await ResolveHighlightAsync(user.Id);

        return new PublicProfileDto(
            DisplayName: user.DisplayName ?? user.Email ?? "Autor(a)",
            Bio: user.Bio,
            AvatarUrl: user.AvatarUrl,
            Slug: user.Slug!,
            Projects: projectSummaries.ToArray(),
            Highlight: highlight
        );
    }

    /* ===================== PRIVATE METHODS ===================== */

 private async Task<PublicProjectSummaryDto> BuildProjectSummaryAsync(Project project, EventDto? activeEvent, Guid userId)
{
    int? eventPercent = null;
    int? eventTotalWritten = null;
    int? eventTargetWords = null;
    string? activeEventName = null;

    IEnumerable<Guid> userIds = [userId];

    if (activeEvent != null)
    {
        var projectEvent = await projectEventsRepository.GetProjectEventByProjectIdAndEventId(project.Id, activeEvent.Id);

        if (projectEvent != null)
        {
            eventTargetWords = projectEvent.TargetWords ?? activeEvent.DefaultTargetWords ?? 50000;
            var totalsByUser =
                await projectProgressRepository.GetTotalWordsByUsersAsync(userIds, activeEvent.StartsAtUtc, activeEvent.EndsAtUtc);
           
            eventTotalWritten = totalsByUser.TryGetValue(userId, out var total) ? total : 0;
            eventPercent = eventTargetWords > 0 ? (int)Math.Min(100, Math.Round(100.0 * eventTotalWritten.Value / eventTargetWords.Value)) : 0;

            activeEventName = activeEvent.Name;
        }
    }

    return new PublicProjectSummaryDto(
        ProjectId: project.Id,
        Title: project.Title ?? "Projeto",
        CurrentWords: project.CurrentWordCount,
        WordGoal: project.WordCountGoal,
        EventPercent: eventPercent,
        EventTotalWritten: eventTotalWritten,
        EventTargetWords: eventTargetWords,
        ActiveEventName: activeEventName
    );
}



    private async Task<string?> ResolveHighlightAsync(Guid userId)
    {
        var recentWin =
            await projectEventsRepository.GetMostRecentWinByUserIdAsync(userId);

        return recentWin != null
            ? $"Winner — {recentWin.Event!.Name}"
            : null;
    }
}
