// Controllers/EventValidationController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventValidationController(IEventValidationService svc, IHttpContextAccessor ctx) : ControllerBase
{
    private Guid CurrentUserId()
    {
        var id = ctx.HttpContext?.User?.FindFirst("sub")?.Value
                 ?? ctx.HttpContext?.User?.FindFirst("user_id")?.Value;
        return Guid.Parse(id!);
    }

    public record PreviewQuery(Guid projectId);
    public record ValidateBody(Guid projectId, int words, string? source);

    [HttpGet("{eventId:guid}/validate/preview")]
    public async Task<IActionResult> Preview(Guid eventId, [FromQuery] PreviewQuery q)
    {
        var (target, total) = await svc.PreviewAsync(CurrentUserId(), eventId, q.projectId);
        return Ok(new { target, total });
    }

    [HttpPost("{eventId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid eventId, [FromBody] ValidateBody body)
    {
        await svc.ValidateAsync(CurrentUserId(), eventId, body.projectId, body.words, body.source ?? "manual");
        return NoContent();
    }
}