using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _svc;

    public EventsController(IEventService svc) => _svc = svc;

    [HttpGet("active")]
    public async Task<ActionResult<EventDto[]>> GetActive()
        => Ok(await _svc.GetActiveAsync());

    [Authorize] // se necessário
    [HttpPost]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventRequest req)
        => Ok(await _svc.CreateAsync(req));

    [Authorize]
    [HttpPost("join")]
    public async Task<ActionResult> Join([FromBody] JoinEventRequest req)
    {
        var pe = await _svc.JoinAsync(req);
        return Ok(new { pe.Id, pe.ProjectId, pe.EventId, pe.TargetWords });
    }

    [HttpGet("{eventId:guid}/projects/{projectId:guid}/progress")]
    public async Task<ActionResult<EventProgressDto>> Progress(Guid eventId, Guid projectId)
        => Ok(await _svc.GetProgressAsync(projectId, eventId));

    [Authorize]
    [HttpPost("finalize")]
    public async Task<ActionResult> Finalize([FromBody] FinalizeRequest req)
    {
        var pe = await _svc.FinalizeAsync(req.ProjectEventId);
        return Ok(new { pe.Id, pe.Won, pe.FinalWordCount, pe.ValidatedAtUtc });
    }
}
