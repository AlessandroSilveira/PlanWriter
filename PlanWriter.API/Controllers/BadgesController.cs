using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Services;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BadgesController(IBadgeServices badgeServices) : ControllerBase
{
    /// <summary>
    /// Get Badges by Project Id
    /// </summary>
    [HttpGet("projectId/{projectId}")]
    public async Task<IActionResult> GetById(Guid projectId)
    {
        var response = await badgeServices.GetBadgesByProjetcId(projectId);
        return Ok(response);
    }
}