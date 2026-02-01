using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Badges.Dtos.Queries;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BadgesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get Badges by Project Id
    /// </summary>
    [HttpGet("projectId/{projectId}")]
    public async Task<IActionResult> GetById(Guid projectId)
    {
        var response = await mediator.Send(new GetByIdQuery(projectId));
      
         return Ok(response);
    }
    
    /// <summary>
    /// Get project badges (calculates/assigns if needed)
    /// </summary>
    [HttpGet("{projectId:guid}/badges")]
    public async Task<IActionResult> GetBadges(Guid projectId)
    {
        var response = await mediator.Send(new GetByProjectIdQuery(projectId));
        
        return Ok(response);
    }
}