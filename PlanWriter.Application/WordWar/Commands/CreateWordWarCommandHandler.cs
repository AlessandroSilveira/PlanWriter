using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Dtos;
using PlanWriter.Application.WordWar.Dtos.Commands;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.Events;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;

namespace PlanWriter.Application.WordWar.Commands;

public class CreateWordWarCommandHandler(ILogger<CreateWordWarCommandHandler> logger,
    IEventReadRepository eventReadRepository,
    IWordWarReadRepository wordWarReadRepository,
    IWordWarRepository wordWarRepository) : IRequestHandler<CreateWordWarCommand, Guid>
{
    public async Task<Guid> Handle(CreateWordWarCommand request, CancellationToken cancellationToken)
    {
        if (request.DurationMinutes <= 0)
        {
            logger.LogError("DurationMinutes must be greater than 0.");
            throw new ValidationException("DurationMinutes must be greater than 0");
        }

        var eventExist = await eventReadRepository.GetEventByIdAsync(request.EventId, cancellationToken);

        if (eventExist is null)
        {
            logger.LogError("Event not found.");
            throw new NotFoundException("Event not found.");
        }

        if (!eventExist.IsActive)
        {
            logger.LogError("The event is no longer active.");
            throw new BusinessRuleException("The event is no longer active.");
        }

        var now = DateTime.UtcNow;
        if (now < eventExist.StartsAtUtc || now > eventExist.EndsAtUtc)
        {
            logger.LogError("The event is outside the valid period.");
            throw new BusinessRuleException("The event is outside the valid period.");
        }

        var wordwar = await wordWarReadRepository.GetActiveByEventIdAsync(request.EventId, cancellationToken);
        if (wordwar?.Status is WordWarStatus.Waiting or WordWarStatus.Running)
        {
            logger.LogError("There is already a word war pending or in progress.");
            throw new BusinessRuleException("There is already a word war pending or in progress.");
        }

        var startsAtUtc = now;
        var endsAtUtc = startsAtUtc.AddMinutes(request.DurationMinutes);

        var warId = await wordWarRepository.CreateAsync(
            request.EventId,
            request.RequestedByUserId,
            request.DurationMinutes,
            startsAtUtc,
            endsAtUtc,
            WordWarStatus.Waiting,
            cancellationToken);

        logger.LogInformation(
            "Word war created in Waiting status. WarId: {WarId}, EventId: {EventId}, RequestedByUserId: {RequestedByUserId}, DurationMinutes: {DurationMinutes}",
            warId,
            request.EventId,
            request.RequestedByUserId,
            request.DurationMinutes);

        return warId;
    }
}
