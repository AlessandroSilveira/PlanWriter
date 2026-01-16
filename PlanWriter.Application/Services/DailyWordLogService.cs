// Application/Services/DailyWordLogService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos.Buddies;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.Application.Services;

public class DailyWordLogService(
    IDailyWordLogRepository repo,
    IUserService userService) : IDailyWordLogService
{
    public async Task UpsertAsync(CreateDailyWordLogRequest req, ClaimsPrincipal user)
    {
        var userId = userService.GetUserId(user);

        var existing = await repo.GetByProjectAndDateAsync(
            req.ProjectId,
            req.Date,
            userId
        );

        if (existing is null)
        {
            await repo.AddAsync(new DailyWordLog
            {
                Id = Guid.NewGuid(),
                ProjectId = req.ProjectId,
                UserId = userId,
                Date = req.Date,
                WordsWritten = req.WordsWritten
            });
        }
        else
        {
            existing.WordsWritten = req.WordsWritten;
            await repo.UpdateAsync(existing);
        }
    }

    public async Task<IEnumerable<DailyWordLogDto>> GetByProjectAsync(Guid projectId, ClaimsPrincipal user)
    {
        var userId = userService.GetUserId(user);

        var logs = await repo.GetByProjectAsync(
            projectId,
            userId
        );

        return logs.Select(x => new DailyWordLogDto
        {
            Date = x.Date,
            WordsWritten = x.WordsWritten
        });
    }
    
}