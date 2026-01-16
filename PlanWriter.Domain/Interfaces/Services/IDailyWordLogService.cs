using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IDailyWordLogService
{
    Task UpsertAsync(CreateDailyWordLogRequest req, ClaimsPrincipal user);
    Task<IEnumerable<DailyWordLogDto>> GetByProjectAsync(Guid projectId, ClaimsPrincipal user);
}