using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.DailyWordLogs.Dtos.Commands;
using PlanWriter.Application.DailyWordLogs.Dtos.Queries;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos.Projects;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/word-logs")]
[Authorize]
public class DailyWordLogsController(Mediator mediator, IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upsert(CreateDailyWordLogRequest req)
    {
        await mediator.Send(new UpsertDailyWordLogCommand(req, User));
        return NoContent();
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<IActionResult> GetByProject(Guid projectId)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new GetByProjectQuery(projectId, userId));
        return Ok(response);
    }
}