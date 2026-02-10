using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Badges;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Badges.Handlers;

public class AssignBadgesOnProgressHandler(IProjectRepository projectRepository, IProjectProgressRepository progressRepository, IBadgeRepository badgeRepository, 
    IProjectReadRepository  projectReadRepository, IProjectProgressReadRepository progressReadRepository, IBadgeReadRepository? badgeReadRepository = null)
    : INotificationHandler<ProjectProgressAdded>
{
    public async Task Handle(ProjectProgressAdded notification, CancellationToken ct)
    {
        // 1️⃣ Projeto (garante ownership)
        var project = await projectReadRepository.GetUserProjectByIdAsync(notification.ProjectId, notification.UserId, ct);

        if (project == null)
            return;

        // 2️⃣ Progresso
        var entries = (await progressReadRepository.GetProgressByProjectIdAsync(notification.ProjectId, notification.UserId, ct))
            .ToList();

        if (entries.Count == 0)
            return;

        // 3️⃣ Badges já existentes
        var existingBadgeNames = badgeReadRepository is null
            ? new HashSet<string>()
            : (await badgeReadRepository.GetByProjectIdAsync(notification.ProjectId, notification.UserId, ct))
                .Select(b => b.Name)
                .ToHashSet();

        var newBadges = new List<Badge>();
        var now = DateTime.UtcNow;

        void AddIfMissing(string name, string description, string icon)
        {
            if (existingBadgeNames.Contains(name))
                return;

            newBadges.Add(new Badge
            {
                ProjectId = notification.ProjectId,
                Name = name,
                Description = description,
                Icon = icon,
                AwardedAt = now
            });
        }

        // ✍️ Primeiro progresso
        AddIfMissing("Primeiro Passo", "Parabéns por registrar seu primeiro progresso!", "✍️");

        // 💯 100 palavras em uma sessão
        if (entries.Any(e => e.WordsWritten >= 100))
            AddIfMissing("Cem Palavras", "Parabéns por escrever 100 palavras em uma única sessão!", "💯");
        

        // 🔟 10 dias distintos
        var distinctDays = entries
            .Select(e => e.Date.Date)
            .Distinct()
            .Count();

        if (distinctDays >= 10)
            AddIfMissing("Dez Dias", "Você escreveu em 10 dias diferentes!", "🔟");
        

        // 🔥 Streak (dias consecutivos)
        var daySet = entries.Select(e => e.Date.Date).Distinct().ToHashSet();

        var streak = 0;
        for (var d = DateTime.UtcNow.Date; daySet.Contains(d); d = d.AddDays(-1))
            streak++;

        if (streak >= 5)
            AddIfMissing("Constância", "5 dias seguidos escrevendo!", "🧠");
        if (streak >= 7)
            AddIfMissing("Streak 7 Dias", "Uma semana inteira escrevendo!", "🔥");
        if (streak >= 14)
            AddIfMissing("Streak 14 Dias", "Duas semanas de constância!", "⚡");
        if (streak >= 30)
            AddIfMissing("Streak 30 Dias", "Um mês sem falhar!", "🏅");

        // 🚀 Meta atingida
        if (project.WordCountGoal.HasValue && project.CurrentWordCount >= project.WordCountGoal.Value)
        {
            AddIfMissing("Meta Atingida", "Você alcançou a meta do projeto!", "🚀");
        }

        // 4️⃣ Persistência
        if (newBadges.Count > 0)
            await badgeRepository.SaveAsync(newBadges);
    }
}
