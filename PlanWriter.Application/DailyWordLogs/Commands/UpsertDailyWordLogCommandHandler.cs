using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.DailyWordLogs.Dtos.Commands;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.DailyWordLogs.Commands;

public class UpsertDailyWordLogCommandHandler(IUserService userService, IDailyWordLogRepository dailyWordLogRepository,
    ILogger<UpsertDailyWordLogCommandHandler> logger)
    : IRequestHandler<UpsertDailyWordLogCommand, Unit>
{

    public async Task<Unit> Handle(UpsertDailyWordLogCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetUserId(request.User);

        var existing = await dailyWordLogRepository
            .GetByProjectAndDateAsync(request.Req.ProjectId, request.Req.Date, userId);

        if (existing is null)
            await InsertAsync(request, userId);
        else
            await UpdateAsync(existing, request.Req.WordsWritten);

        return Unit.Value;
    }
    
    private Task InsertAsync(UpsertDailyWordLogCommand request, Guid userId)
    {
        var log = new DailyWordLog
        {
            Id = Guid.NewGuid(),
            ProjectId = request.Req.ProjectId,
            UserId = userId,
            Date = request.Req.Date,
            WordsWritten = request.Req.WordsWritten
        };

        return dailyWordLogRepository.AddAsync(log);
    }

    private Task UpdateAsync(DailyWordLog existing, int wordsWritten)
    {
        existing.WordsWritten = wordsWritten;
        return dailyWordLogRepository.UpdateAsync(existing);
    }
}