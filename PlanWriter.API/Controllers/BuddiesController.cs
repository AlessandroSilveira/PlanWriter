// PlanWriter.API/Controllers/BuddiesController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BuddiesController : ControllerBase
{
    private readonly IBuddiesService _service;
    public BuddiesController(IBuddiesService service) => _service = service;

    // Ajuste conforme como você emite o ID do usuário nos tokens
    private Guid Me => Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<ActionResult<List<BuddiesDto.BuddySummaryDto>>> Get(CancellationToken ct)
        => Ok(await _service.ListAsync(Me, ct));

    [HttpPost("follow/username")]
    public async Task<IActionResult> FollowByUsername([FromBody] BuddiesDto.FollowBuddyByUsernameRequest req, CancellationToken ct)
    {
        await _service.FollowByUsernameAsync(Me, req.Username, ct);
        return NoContent();
    }

    [HttpPost("follow/{followeeId:guid}")]
    public async Task<IActionResult> FollowById(Guid followeeId, CancellationToken ct)
    {
        await _service.FollowByIdAsync(Me, followeeId, ct);
        return NoContent();
    }

    [HttpDelete("{followeeId:guid}")]
    public async Task<IActionResult> Unfollow(Guid followeeId, CancellationToken ct)
    {
        await _service.UnfollowAsync(Me, followeeId, ct);
        return NoContent();
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<List<BuddiesDto.BuddyLeaderboardItemDto>>> Leaderboard([FromQuery] Guid? eventId, [FromQuery] DateOnly? start, [FromQuery] DateOnly? end, CancellationToken ct)
        => Ok(await _service.LeaderboardAsync(Me, eventId, start, end, ct));
}