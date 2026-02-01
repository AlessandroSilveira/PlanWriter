using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Milestones.Handlers;

public class CompleteMilestonesOnProgressHandler(IMilestonesRepository milestonesRepository)
    : INotificationHandler<ProjectProgressAdded>
{
    public async Task Handle(ProjectProgressAdded notification, CancellationToken ct)
    {
        var milestones = await milestonesRepository.GetByProjectIdAsync(notification.ProjectId);

        var now = DateTime.UtcNow;

        foreach (var milestone in milestones.Where(m => !m.Completed))
        {
            if (notification.NewTotal < milestone.TargetAmount) continue;
            milestone.Completed = true;
            milestone.CompletedAt = now;

            await milestonesRepository.UpdateAsync(milestone, ct);
        }
    }
}