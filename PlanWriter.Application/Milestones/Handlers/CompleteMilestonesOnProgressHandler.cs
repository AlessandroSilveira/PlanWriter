using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Milestones;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Milestones.Handlers;

public class CompleteMilestonesOnProgressHandler(IMilestonesReadRepository milestonesReadRepository, IMilestonesRepository  milestonesRepository)
    : INotificationHandler<ProjectProgressAdded>
{
    public async Task Handle(ProjectProgressAdded notification, CancellationToken ct)
    {
        var milestones = await milestonesReadRepository.GetByProjectIdAsync(notification.ProjectId, ct);

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