using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.DTO;
using PlanWriter.Application.DTOs;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserService userService, IAuthService authService, IProjectService projectService)
    : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var result = await userService.RegisterUserAsync(dto);

        if (!result)
            return BadRequest("Email already exists.");

        return Ok("User registered successfully.");
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        var token = await authService.LoginAsync(dto);

        if (token == null)
            return Unauthorized("Invalid email or password.");

        return Ok(new { AccessToken = token });
    }

   
}