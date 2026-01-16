// API/Controllers/DailyWordLogsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/word-logs")]
[Authorize]
public class DailyWordLogsController(IDailyWordLogService service) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> Upsert(CreateDailyWordLogRequest req)
    {
        await service.UpsertAsync(req, User);
        return NoContent();
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<IActionResult> GetByProject(Guid projectId)
    {
        var logs = await service.GetByProjectAsync(projectId, User);
        return Ok(logs);
    }
}