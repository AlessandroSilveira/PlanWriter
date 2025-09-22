using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet("active")]
    public async Task<ActionResult<EventDto[]>> GetActive()
        => Ok(await eventService.GetActiveAsync());

    [Authorize] // se necessário
    [HttpPost]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventRequest req)
        => Ok(await eventService.CreateAsync(req));

    [Authorize]
    [HttpPost("join")]
    public async Task<ActionResult> Join([FromBody] JoinEventRequest req)
    {
        var pe = await eventService.JoinAsync(req);
        return Ok(new { pe.Id, pe.ProjectId, pe.EventId, pe.TargetWords });
    }

    [HttpGet("{eventId:guid}/projects/{projectId:guid}/progress")]
    public async Task<ActionResult<EventProgressDto>> Progress(Guid eventId, Guid projectId)
        => Ok(await eventService.GetProgressAsync(projectId, eventId));

    [Authorize]
    [HttpPost("finalize")]
    public async Task<ActionResult> Finalize([FromBody] FinalizeRequest req)
    {
        var pe = await eventService.FinalizeAsync(req.ProjectEventId);
        return Ok(new { pe.Id, pe.Won, pe.FinalWordCount, pe.ValidatedAtUtc });
    }

    [HttpGet("{eventId:guid}/leaderboard")]
    public async Task<IActionResult> Leaderboard(Guid eventId, [FromQuery] string scope = "total",
        [FromQuery] int top = 50)
    {
        var response = await eventService.GetLeaberBoard(eventId, scope, top);
        
        return Ok(response);
    }
}
