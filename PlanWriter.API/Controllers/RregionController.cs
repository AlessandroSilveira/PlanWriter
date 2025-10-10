// PlanWriter.API/Controllers/RegionsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegionsController(IRegionsService regionsService) : ControllerBase
{
    [HttpGet("leaderboard")]
    public async Task<ActionResult<IEnumerable<RegionLeaderboardDto>>> Leaderboard(CancellationToken ct)
    {
        var res = await regionsService.GetLeaderboardAsync(ct);
        return Ok(res);
    }
}