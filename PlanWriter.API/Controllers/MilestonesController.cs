using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/milestones")]
[Authorize]
public class MilestonesController(IMilestonesService svc, IUserService userSvc) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MilestoneDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct)
    {
        var data = await svc.GetProjectMilestonesAsync(projectId, User, ct);
        return Ok(data);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MilestoneDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateMilestoneDto dto, CancellationToken ct)
    {
        var created = await svc.CreateAsync(projectId, User, dto, ct);
        return CreatedAtAction(nameof(List), new { projectId }, created);
    }

    [HttpDelete("{milestoneId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid milestoneId, CancellationToken ct)
    {
        await svc.DeleteAsync(milestoneId, User, ct);
        return NoContent();
    }
}