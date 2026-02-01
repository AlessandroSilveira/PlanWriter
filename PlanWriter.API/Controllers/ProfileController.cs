using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.Profile.Dtos.Commands;
using PlanWriter.Application.Profile.Dtos.Queries;
using PlanWriter.Domain.Dtos;

using PlanWriter.Domain.Requests;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(IUserService userService, IMediator mediator) : ControllerBase
{

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MyProfileDto>> GetMine()
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new GetMineProfileQuery(userId)); 
        return Ok(response);
    }


    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<MyProfileDto>> UpdateMine([FromBody] UpdateMyProfileRequest request)
    {
        var userId = userService.GetUserId(User);
        var response = await mediator.Send(new UpdateProfileCommand(userId, request));
        return Ok(response);
    }
         

    // Público
    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicProfileDto>> GetPublic(string slug)
    {
        var response = await mediator.Send(new GetPublicProfileQuery(slug)); //await profileService.GetPublicAsync(slug);
        return Ok();
    }
       
}