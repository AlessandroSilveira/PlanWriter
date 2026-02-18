using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.WordWar.Dtos.Commands;
using PlanWriter.Application.WordWar.Dtos.Queries;
using PlanWriter.Application.WordWar.Queries;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventWordWarsController(IUserService userService, IMediator mediator) : ControllerBase
{
    [Authorize]
    [HttpPost("{eventId:guid}/wordwars")]
    public async Task<ActionResult<Guid>> Create(Guid eventId, [FromBody] CreateWordWarRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new CreateWordWarCommand(eventId, request.DurationMinutes, userId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/create")]
    public async Task<ActionResult<Guid>> CreateLegacy([FromBody] CreateWordWarLegacyRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new CreateWordWarCommand(request.EventId, request.DurationMinutes, userId));
        return Ok(response);
    }

    [Authorize]
    [HttpGet("{eventId:guid}/wordwars/active")]
    public async Task<ActionResult<WordWarDto?>> ActiveByEvent(Guid eventId)
    {
        var response = await mediator.Send(new GetActiveWordWarByEventIdQuery(Guid.Empty, eventId));
        return Ok(response);
    }

    [Authorize]
    [HttpGet("wordwars/active")]
    public async Task<ActionResult<WordWarDto?>> ActiveByQuery([FromQuery] Guid eventId)
    {
        var response = await mediator.Send(new GetActiveWordWarByEventIdQuery(Guid.Empty, eventId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/{warId:guid}/join")]
    public async Task<ActionResult<bool>> JoinByRoute(Guid warId, [FromBody] JoinWordWarRouteRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new JoinWordWarCommand(warId, userId, request.ProjectId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/join")]
    public async Task<ActionResult<bool>> JoinByBody([FromBody] JoinWordWarBodyRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new JoinWordWarCommand(request.WarId, userId, request.ProjectId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/{warId:guid}/leave")]
    public async Task<ActionResult<bool>> LeaveByRoute(Guid warId)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new LeaveWordWarCommand(warId, userId)); 
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/leave")]
    public async Task<ActionResult<bool>> LeaveByBody([FromBody] WordWarIdRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new LeaveWordWarCommand(request.WarId, userId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/{warId:guid}/start")]
    public async Task<ActionResult> StartByRoute(Guid warId)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new StartWordWarCommand(warId, userId)); 
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/start")]
    public async Task<ActionResult> StartByBody([FromBody] WordWarIdRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new StartWordWarCommand(request.WarId, userId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/{warId:guid}/finish")]
    public async Task<ActionResult> FinishByRoute(Guid warId)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new FinishWordWarCommand(warId, userId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/finish")]
    public async Task<ActionResult> FinishByBody([FromBody] WordWarIdRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new FinishWordWarCommand(request.WarId, userId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/{warId:guid}/checkpoint")]
    public async Task<ActionResult<bool>> CheckpointByRoute(Guid warId, [FromBody] WordWarCheckpointRouteRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new SubmitWordWarCheckpointCommand(warId, userId, request.WordsInRound));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("wordwars/checkpoint")]
    public async Task<ActionResult<bool>> CheckpointByBody([FromBody] WordWarCheckpointBodyRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new SubmitWordWarCheckpointCommand(request.WarId, userId, request.WordsInRound));
        return Ok(response);
    }

    [Authorize]
    [HttpGet("wordwars/{warId:guid}/scoreboard")]
    public async Task<ActionResult> ScoreBoardByRoute(Guid warId)
    {
        var response = await mediator.Send(new GetWordWarScoreboardQuery(warId));
        return Ok(response);
    }

    [Authorize]
    [HttpGet("wordwars/scoreboard")]
    public async Task<ActionResult> ScoreBoardByQuery([FromQuery] Guid warId)
    {
        var response = await mediator.Send(new GetWordWarScoreboardQuery(warId));
        return Ok(response);
    }

    public sealed record CreateWordWarRequest(int DurationMinutes);
    public sealed record CreateWordWarLegacyRequest(Guid EventId, int DurationMinutes);
    public sealed record JoinWordWarRouteRequest(Guid ProjectId);
    public sealed record JoinWordWarBodyRequest(Guid WarId, Guid ProjectId);
    public sealed record WordWarIdRequest(Guid WarId);
    public sealed record WordWarCheckpointRouteRequest(int WordsInRound);
    public sealed record WordWarCheckpointBodyRequest(Guid WarId, int WordsInRound);
}
