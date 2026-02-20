using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlanWriter.API.Security;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Dtos.Auth;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IMediator mediator,
    ILoginLockoutService loginLockoutService,
    TimeProvider timeProvider,
    ILogger<AuthController> logger)
    : ControllerBase
{
    private const string GenericAuthError = "Não foi possível autenticar no momento.";

    [EnableRateLimiting("auth-register")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
    {
        var result = await mediator.Send(new RegisterUserCommand(request));
        if (!result)
            return BadRequest();
        
        return Ok("User registered successfully.");

    }
    
    [EnableRateLimiting("auth-login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var device = Request.Headers.UserAgent.ToString();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var preCheck = loginLockoutService.Check(email, ipAddress, now);
        if (preCheck.IsLocked)
        {
            logger.LogWarning(
                "Blocked login attempt during lockout for {Email} from {IpAddress}. LockedUntilUtc={LockedUntilUtc}",
                email,
                ipAddress ?? "unknown",
                preCheck.LockedUntilUtc);

            return StatusCode(StatusCodes.Status403Forbidden, GenericAuthError);
        }

        var tokens = await mediator.Send(new LoginUserCommand(request, ipAddress, device));
        if (tokens is null)
        {
            var failState = loginLockoutService.RegisterFailure(email, ipAddress, now);
            if (failState.IsLocked)
            {
                logger.LogWarning(
                    "Lockout activated for {Email} from {IpAddress}. UserFailures={UserFailures} IpFailures={IpFailures} LockedUntilUtc={LockedUntilUtc}",
                    email,
                    ipAddress ?? "unknown",
                    failState.UserFailureCount,
                    failState.IpFailureCount,
                    failState.LockedUntilUtc);

                return StatusCode(StatusCodes.Status403Forbidden, GenericAuthError);
            }

            return Unauthorized(GenericAuthError);
        }

        loginLockoutService.RegisterSuccess(email, ipAddress);
        
        return Ok(tokens);
    }

    [EnableRateLimiting("auth-refresh")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var device = Request.Headers.UserAgent.ToString();

        var tokens = await mediator.Send(new RefreshSessionCommand(dto, ipAddress, device));
        if (tokens is null)
        {
            return Unauthorized(GenericAuthError);
        }

        return Ok(tokens);
    }

    [EnableRateLimiting("auth-refresh")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        await mediator.Send(new LogoutSessionCommand(dto));
        return Ok(new { message = "Sessão encerrada com sucesso." });
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
