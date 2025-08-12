using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.DTO;
using PlanWriter.Application.DTOs;
using PlanWriter.Application.Interfaces;

namespace PlanWriter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;

    public AuthController(IUserService userService, IAuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var result = await _userService.RegisterUserAsync(dto);

        if (!result)
            return BadRequest("Email already exists.");

        return Ok("User registered successfully.");
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        var token = await _authService.LoginAsync(dto);

        if (token == null)
            return Unauthorized("Invalid email or password.");

        return Ok(new { AccessToken = token });
    }
}