using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Domain.Interfaces.ReadModels;

namespace PlanWriter.Application.Projects.Queries;

public class GetMonthlyProgressQueryHandler(ILogger<GetMonthlyProgressQueryHandler> logger, IProjectProgressReadRepository projectProgressReadRepository )
    : IRequestHandler<GetMonthlyProgressQuery, int>
{
    public async Task<int> Handle(GetMonthlyProgressQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting monthly progress for user {UserId}", request.UserId);

        var nowUtc = DateTime.Now;
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEndUtc = monthStartUtc.AddMonths(1);

        logger.LogInformation("Getting monthly progress for user {UserId} between {Start} and {End}", request.UserId, monthStartUtc, monthEndUtc);

        var total =
            await projectProgressReadRepository.GetMonthlyWordsAsync(request.UserId, monthStartUtc, monthEndUtc, cancellationToken);

        logger.LogInformation("Monthly progress for user {UserId} is {TotalWords}", request.UserId, total);

        return total;
    }
}