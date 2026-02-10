using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.DailyWordLogs.Dtos.Commands;
using PlanWriter.Domain.Interfaces.Repositories.DailyWordLogWrite;

namespace PlanWriter.Application.DailyWordLogs.Commands;

public class UpsertDailyWordLogCommandHandler(IDailyWordLogWriteRepository writeRepository, ILogger<UpsertDailyWordLogCommandHandler> logger
) : IRequestHandler<UpsertDailyWordLogCommand, Unit>
{
    public async Task<Unit> Handle(UpsertDailyWordLogCommand request, CancellationToken ct)
    {
        Validate(request);

        logger.LogInformation(
            "Upserting DailyWordLog. ProjectId={ProjectId} UserId={UserId} Date={Date} Words={WordsWritten}",
            request.Req.ProjectId,
            request.UserId,
            request.Req.Date,
            request.Req.WordsWritten
        );

        await writeRepository.UpsertAsync(request.Req.ProjectId, request.UserId, request.Req.Date, request.Req.WordsWritten, ct);

        return Unit.Value;
    }

    private static void Validate(UpsertDailyWordLogCommand request)
    {
        if (request.UserId == Guid.Empty)
            throw new InvalidOperationException("UserId inválido.");

        if (request.Req.ProjectId == Guid.Empty)
            throw new InvalidOperationException("ProjectId inválido.");

        if (request.Req.WordsWritten < 0)
            throw new InvalidOperationException("WordsWritten não pode ser negativo.");

        if (request.Req.Date == default)
            throw new InvalidOperationException("Date inválida.");
    }
}