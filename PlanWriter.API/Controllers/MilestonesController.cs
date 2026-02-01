using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.Milestones.Dtos.Commands;
using PlanWriter.Application.Milestones.Dtos.Queries;
using PlanWriter.Domain.Dtos;


namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/milestones")]
[Authorize]
public class MilestonesController(IUserService userService, IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MilestoneDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid projectId)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new GetProjectMilestonesQuery(projectId, userId));
        
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MilestoneDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateMilestoneDto dto)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new CreateMilestoneCommand(projectId, userId, dto));
        return CreatedAtAction(nameof(List), new { projectId }, response);
    }

    [HttpDelete("{milestoneId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid milestoneId)
    {
        var userId = userService.GetUserId(User);
        await mediator.Send(new DeleteMilestoneCommand(projectId, milestoneId, userId));
        return NoContent();
    }
}