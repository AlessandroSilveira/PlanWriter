
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Buddies.Dtos.Commands;
using PlanWriter.Application.Buddies.Dtos.Queries;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Buddies;


namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BuddiesController( IUserService userService, IMediator mediator) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<ActionResult<List<BuddiesDto.BuddySummaryDto>>> Get()
    {
        var response = await mediator.Send(new GetListBuddiesQuery(Me));
        return Ok(response);
    }
       

    [HttpPost("follow/username")]
    public async Task<IActionResult> FollowByUsername([FromBody] BuddiesDto.FollowBuddyByUsernameRequest req)
    {
        await mediator.Send(new FollowByUsernameCommand(Me, req.Email));
        return NoContent();
    }

    [HttpPost("follow/{followeeId:guid}")]
    public async Task<IActionResult> FollowById(Guid followeeId, CancellationToken ct)
    {
        await mediator.Send(new FollowByIdCommand(Me, followeeId), ct);
        return NoContent();
    }

    [HttpDelete("{followeeId:guid}")]
    public async Task<IActionResult> Unfollow(Guid followeeId, CancellationToken ct)
    {
        await mediator.Send(new UnfollowCommand(Me, followeeId), ct);
        return NoContent();
    }
    
    [Authorize]
    [HttpGet("leaderboard")]
    public async Task<IActionResult> BuddiesLeaderboard([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new BuddiesLeaderboardQuery(userId, start, end));
        return Ok(response);
    }
}