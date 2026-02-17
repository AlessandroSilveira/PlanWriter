using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Goodies.Dtos.Queries;
using PlanWriter.Application.Interfaces;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public sealed class EventGoodiesController(IMediator mediator, IUserService userService) : ControllerBase
{
    [HttpGet("{eventId:guid}/projects/{projectId:guid}/goodies")]
    public async Task<IActionResult> GetEventGoodies(Guid eventId, Guid projectId)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new GetEventGoodiesQuery(userId, eventId, projectId));
        return Ok(response);
    }
}
