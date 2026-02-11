using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.DailyWordLogs.Dtos.Commands;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories.DailyWordLogWrite;

namespace PlanWriter.Application.DailyWordLogs.Commands;

public class UpsertDailyWordLogCommandHandler(
    IDailyWordLogWriteRepository writeRepository,
    IProjectReadRepository projectReadRepository,
    IMediator mediator,
    ILogger<UpsertDailyWordLogCommandHandler> logger
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

        var project = await projectReadRepository.GetUserProjectByIdAsync(
                          request.Req.ProjectId,
                          request.UserId,
                          ct
                      )
                      ?? throw new UnauthorizedAccessException("Project not found.");

        await writeRepository.UpsertAsync(request.Req.ProjectId, request.UserId, request.Req.Date, request.Req.WordsWritten, ct);

        var logs = await writeRepository.GetByProjectAsync(request.Req.ProjectId, request.UserId, ct);
        var newTotal = logs.Sum(x => Math.Max(0, x.WordsWritten));

        await mediator.Publish(
            new ProjectProgressAdded(project.Id, request.UserId, newTotal, project.GoalUnit),
            ct
        );

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
