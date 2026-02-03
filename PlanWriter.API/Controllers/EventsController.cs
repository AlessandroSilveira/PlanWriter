using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Events.Dtos.Commands;
using PlanWriter.Application.Events.Dtos.Queries;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos.Events;


namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IUserService userService, IMediator mediator) : ControllerBase
{
    // lista eventos ativos
    [HttpGet("active")]
    public async Task<ActionResult<EventDto[]>> GetActive()
    {
        var response = await mediator.Send(new GetActiveEventsQuery());
        return Ok(response);
    }
    
    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid eventId)
    {
        var response = await mediator.Send(new GetEventByIdQuery(eventId));
        return response is null ? NotFound(new { message = "Evento não encontrado." }) : Ok(response);
    }
    
    [Authorize]
    [HttpPost("join")]
    public async Task<ActionResult> Join([FromBody] JoinEventRequest req)
    {
        var response = await mediator.Send(new JoinEventCommand(req));
        return Ok(response);
    }
    
    [Authorize]
    [HttpGet("{eventId:guid}/projects/{projectId:guid}/progress")]
    public async Task<ActionResult<EventProgressDto>> GetProgress(Guid eventId, Guid projectId)
    {
        var response = await mediator.Send(new GetEventProgressQuery(eventId, projectId));
        return Ok(response);
    }
    
    [Authorize]
    [HttpPost("finalize")]
    public async Task<IActionResult> Finalize([FromBody] FinalizeRequest req)
    {
        var response = await mediator.Send(new FinalizeEventCommand(req));
        return Ok(new { response.Id, response.Won, response.FinalWordCount, response.ValidatedAtUtc });
    }
   
    [Authorize]
    [HttpDelete("{eventId:guid}/projects/{projectId:guid}")]
    public async Task<IActionResult> Leave(Guid eventId, Guid projectId)
    {
        await mediator.Send(new LeaveEventCommand(projectId, eventId));
        return NoContent();
    }
    
    [HttpGet("{eventId:guid}/leaderboard")]
    public async Task<IActionResult> Leaderboard(Guid eventId, [FromQuery] string scope = "total", [FromQuery] int top = 50)
    {
        var response = await mediator.Send(new GetEventLeaderboardQuery(eventId, scope, top));
        return Ok(response);
    }
    
    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<MyEventDto>>> GetMyEvents()
    {
        var userId = userService.GetUserId(User);
        var result = await mediator.Send(new GetMyEventsQuery(userId));

        return Ok(result);
    }
}