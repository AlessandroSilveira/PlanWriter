using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Badges.Dtos.Queries;
using PlanWriter.Application.Interfaces;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BadgesController(IMediator mediator, IUserService userService) : ControllerBase
{
    /// <summary>
    /// Get Badges by Project Id
    /// </summary>
    [HttpGet("projectId/{projectId}")]
    public async Task<IActionResult> GetById(Guid projectId)
    {
        var userId =  userService.GetUserId(User);
        var response = await mediator.Send(new GetBadgesByProjectIdQuery(projectId, userId));
      
         return Ok(response);
    }
}