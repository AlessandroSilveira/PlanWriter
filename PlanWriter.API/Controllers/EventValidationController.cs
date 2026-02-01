using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.EventValidation.Dtos.Commands;
using PlanWriter.Application.EventValidation.Dtos.Queries;
using PlanWriter.Application.Interfaces;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventValidationController(IUserService userService, IMediator mediator) : ControllerBase
{
    private Guid UserId => userService.GetUserId(User);
    [HttpGet("{eventId:guid}/validate/preview")]
    public async Task<IActionResult> Preview(Guid eventId, [FromQuery] PreviewQuery q)
    {
        
        var (target, total) = await mediator.Send(new PreviewQuery(UserId, eventId, q.ProjectId)); 
        return Ok(new { target, total });
    }

    [HttpPost("{eventId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid eventId, [FromBody] ValidateRequest request)
    {
        await mediator.Send(new ValidateCommand(UserId, eventId, request.ProjectId, request.Words,
            request.Source ?? "manual"));
        return NoContent();
    }
    
   
}