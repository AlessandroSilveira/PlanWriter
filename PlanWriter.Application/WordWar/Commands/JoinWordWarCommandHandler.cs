using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Dtos.Commands;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;

namespace PlanWriter.Application.WordWar.Commands;

public class JoinWordWarCommandHandler(ILogger<JoinWordWarCommandHandler> logger,
    IWordWarReadRepository wordWarReadRepository,
    IProjectReadRepository projectReadRepository, 
    IWordWarParticipantReadRepository wordWarParticipantReadRepository,
    IWordWarRepository wordWarRepository) : IRequestHandler<JoinWordWarCommand, bool>
{
    public async Task<bool> Handle(JoinWordWarCommand request, CancellationToken cancellationToken)
    {
        var wordWar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);

        if (wordWar is null)
        {
            logger.LogError("WordWar not exist.");
            throw new NotFoundException("WordWar not exist.");
        }

        if (wordWar.Status != WordWarStatus.Waiting)
        {
            logger.LogError("Can't join the word war when the status is not waiting.");
            throw new BusinessRuleException("Can't join the word war when the status is not waiting.");
        }

        var userProject = await projectReadRepository.GetUserProjectsAsync(request.UserId, cancellationToken);
        var project = userProject.FirstOrDefault(a => a.Id == request.ProjectId);
        if (project is null)
        {
            logger.LogError("This project doesn't belong to this user.");
            throw new BusinessRuleException("This project doesn't belong to this user.");
        }

        var participant =
            await wordWarParticipantReadRepository.GetParticipant(request.WarId, request.UserId, cancellationToken);

        if (participant is not null)
        {
            logger.LogInformation(
                "Join treated as idempotent because user already participates. WarId: {WarId}, UserId: {UserId}, ProjectId: {ProjectId}",
                request.WarId,
                request.UserId,
                request.ProjectId);
            return true;
        }

        var insertParticipant =
            await wordWarRepository.JoinAsync(request.WarId, request.UserId, request.ProjectId, cancellationToken);

        if (insertParticipant == 1)
        {
            logger.LogInformation(
                "User joined word war. WarId: {WarId}, UserId: {UserId}, ProjectId: {ProjectId}",
                request.WarId,
                request.UserId,
                request.ProjectId);
            return true;
        }

        var participantAfterJoin =
            await wordWarParticipantReadRepository.GetParticipant(request.WarId, request.UserId, cancellationToken);

        if (participantAfterJoin is not null)
        {
            logger.LogInformation(
                "Join treated as idempotent after concurrent insert. WarId: {WarId}, UserId: {UserId}, ProjectId: {ProjectId}",
                request.WarId,
                request.UserId,
                request.ProjectId);
            return true;
        }

        var latestWordWar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);
        if (latestWordWar is null)
        {
            logger.LogError("WordWar not exist.");
            throw new NotFoundException("WordWar not exist.");
        }

        if (latestWordWar.Status != WordWarStatus.Waiting)
        {
            logger.LogWarning(
                "Join rejected because word war status changed concurrently. WarId: {WarId}, UserId: {UserId}, Status: {Status}",
                request.WarId,
                request.UserId,
                latestWordWar.Status);
            throw new BusinessRuleException("Can't join the word war when the status is not waiting.");
        }

        logger.LogError("Unable to join word war due to state conflict.");
        throw new BusinessRuleException("Unable to join word war due to state conflict.");
    }
}
