using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Milestones.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Milestones.Commands;

public class DeleteMilestoneCommandHandler(
    ILogger<DeleteMilestoneCommandHandler> logger, 
    IMilestonesRepository milestonesRepository)
    : IRequestHandler<DeleteMilestoneCommand, Unit>
{
    public async Task<Unit> Handle(DeleteMilestoneCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting milestone {MilestoneId} for user {UserId}", request.MilestoneId, request.UserId);

        await milestonesRepository.DeleteAsync(request.MilestoneId, request.UserId, cancellationToken);

        logger.LogInformation("Milestone {MilestoneId} deleted for user {UserId}", request.MilestoneId, request.UserId);

        return Unit.Value;
    }
}