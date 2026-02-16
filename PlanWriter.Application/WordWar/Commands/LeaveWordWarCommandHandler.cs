using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Dtos.Commands;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;

namespace PlanWriter.Application.WordWar.Commands;

public class LeaveWordWarCommandHandler(ILogger<LeaveWordWarCommandHandler> logger, 
    IWordWarReadRepository wordWarReadRepository,
    IWordWarParticipantReadRepository wordWarParticipantReadRepository,
    IWordWarRepository wordWarRepository) : IRequestHandler<LeaveWordWarCommand, bool>
{
    public async Task<bool> Handle(LeaveWordWarCommand request, CancellationToken cancellationToken)
    {
        
        var wordWar = await wordWarReadRepository.GetByIdAsync(request.WarId, cancellationToken);

        if (wordWar is null)
        {
            logger.LogError("WordWar not exist.");
            throw new NotFoundException("WordWar not exist.");
        }
        
        if (wordWar.Status != WordWarStatus.Waiting)
        {
            logger.LogError("Can't leave the word war when the status is not waiting.");
            throw new BusinessRuleException("Can't leave the word war when the status is not waiting.");
        }
        
        var participant =
            await wordWarParticipantReadRepository.GetParticipant(request.WarId, request.UserId, cancellationToken);

        if (participant is null)
        {
            logger.LogInformation("The user is not participating in this word war.");
            return true;
        }

        var leaving = await wordWarRepository.LeaveAsync(request.WarId, request.UserId, cancellationToken);

        if (leaving == 1)
            return true;

        var participantAfterLeave =
            await wordWarParticipantReadRepository.GetParticipant(request.WarId, request.UserId, cancellationToken);

        if (participantAfterLeave is null)
            return true;

        logger.LogError("Unable to leave word war due to state conflict.");
        throw new BusinessRuleException("Unable to leave word war due to state conflict.");
    }
}
