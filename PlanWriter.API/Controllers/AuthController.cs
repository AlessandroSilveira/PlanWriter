using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Dtos.Auth;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator)
    : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
    {
        var result = await mediator.Send(new RegisterUserCommand(request));
        if (!result)
            return BadRequest();
        
        return Ok("User registered successfully.");

    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto request)
    {
        var token = await mediator.Send(new LoginUserCommand(request));
        if (string.IsNullOrEmpty(token))
            return Unauthorized("Invalid email or password.");
        
        return Ok(new { AccessToken = token });
    }
    
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var token = await mediator.Send(new ChangePasswordCommand(userId, dto));

            return Ok(new { token });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
   
}