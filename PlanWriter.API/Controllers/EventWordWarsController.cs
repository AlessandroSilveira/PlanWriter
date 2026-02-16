using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.WordWar.Dtos.Commands;
using PlanWriter.Application.WordWar.Dtos.Queries;
using PlanWriter.Application.WordWar.Queries;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.API.Controllers;

public class EventWordWarsController(IUserService userService, IMediator mediator) : ControllerBase
{
    [Authorize]
    [HttpPost("Create")]
    public async Task<ActionResult<MyProfileDto>> Create([FromBody] Guid eventId, int durationsInMinutes )
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new CreateWordWarCommand(eventId, durationsInMinutes, userId)); 
        return Ok(response);
    }
    
    [Authorize]
    [HttpGet("Active")]
    public async Task<ActionResult<MyProfileDto>> Active([FromBody] Guid warId, Guid eventId )
    {
        var response = await mediator.Send(new GetActiveWordWarByEventIdQuery(warId, warId)); 
        return Ok(response);
    }
    
    [Authorize]
    [HttpPost("Join")]
    public async Task<ActionResult<MyProfileDto>> Join([FromBody] Guid warId, Guid projectId )
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new JoinWordWarCommand(warId, userId, projectId)); 
        return Ok(response);
    }
    
    [Authorize]
    [HttpPost("Leave")]
    public async Task<ActionResult<MyProfileDto>> Leave([FromBody] Guid warId)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new LeaveWordWarCommand(warId, userId)); 
        return Ok(response);
    }
    
    [Authorize]
    [HttpPost("Start")]
    public async Task<ActionResult<MyProfileDto>> Start([FromBody] Guid warId)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new StartWordWarCommand(warId, userId)); 
        return Ok(response);
    }
    
    [Authorize]
    [HttpPost("checkpoint")]
    public async Task<ActionResult<MyProfileDto>> Checkpoint([FromBody] Guid warId, int wordsInRound)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new SubmitWordWarCheckpointCommand(warId, userId, wordsInRound)); 
        return Ok(response);
    }
    
    [Authorize]
    [HttpGet("scoreboard")]
    public async Task<ActionResult<MyProfileDto>> ScoreBoard([FromBody] Guid warId)
    {
        var response = await mediator.Send(new GetWordWarScoreboardQuery(warId)); 
        return Ok(response);
    }
}