using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlanWriter.API.Middleware;
using PlanWriter.API.Security;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Dtos.Auth;
using PlanWriter.Domain.Interfaces.Repositories.Auth;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IMediator mediator,
    ILoginLockoutService loginLockoutService,
    IAuthAuditRepository authAuditRepository,
    TimeProvider timeProvider,
    ILogger<AuthController> logger)
    : ControllerBase
{
    private const string GenericAuthError = "Não foi possível autenticar no momento.";

    [EnableRateLimiting("auth-register")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
    {
        try
        {
            var result = await mediator.Send(new RegisterUserCommand(request));
            if (!result)
            {
                await AuditAsync("Register", "Failure", null, "MediatorReturnedFalse");
                return BadRequest();
            }

            await AuditAsync("Register", "Success", null, null);
            return Ok("User registered successfully.");
        }
        catch (InvalidOperationException)
        {
            await AuditAsync("Register", "Failure", null, "InvalidOperation");
            throw;
        }

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
            await AuditAsync("Lockout", "Blocked", null, "PreCheckLocked");
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
            await AuditAsync("Login", "Failure", null, "InvalidCredentials");
            if (failState.IsLocked)
            {
                await AuditAsync("Lockout", "Activated", null, "ThresholdReached");
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
        await AuditAsync("Login", "Success", TryGetUserIdFromAccessToken(tokens.AccessToken), null);
        
        return Ok(tokens);
    }

    [EnableRateLimiting("auth-refresh")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var device = Request.Headers.UserAgent.ToString();

        try
        {
            var tokens = await mediator.Send(new RefreshSessionCommand(dto, ipAddress, device));
            if (tokens is null)
            {
                await AuditAsync("Refresh", "Failure", null, "InvalidOrExpiredRefreshToken");
                return Unauthorized(GenericAuthError);
            }

            await AuditAsync("Refresh", "Success", TryGetUserIdFromAccessToken(tokens.AccessToken), null);
            return Ok(tokens);
        }
        catch (UnauthorizedAccessException)
        {
            await AuditAsync("Refresh", "Failure", null, "RefreshTokenReuseDetected");
            throw;
        }
    }

    [EnableRateLimiting("auth-refresh")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        await mediator.Send(new LogoutSessionCommand(dto));
        await AuditAsync("Logout", "Success", null, null);
        return Ok(new { message = "Sessão encerrada com sucesso." });
    }

    [Authorize]
    [EnableRateLimiting("auth-refresh")]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var device = Request.Headers.UserAgent.ToString();
        var revokedSessions = await mediator.Send(new LogoutAllSessionsCommand(userId, ipAddress, device));

        return Ok(new
        {
            message = "Todas as sessões foram encerradas com sucesso.",
            revokedSessions
        });
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
            await AuditAsync("ChangePassword", "Success", userId, null);

            return Ok(new { token });
        }
        catch (InvalidOperationException ex)
        {
            await AuditAsync("ChangePassword", "Failure", userId, "InvalidOperation");
            return BadRequest(ex.Message);
        }
    }

    private async Task AuditAsync(string eventType, string result, Guid? userId, string? details)
    {
        try
        {
            await authAuditRepository.CreateAsync(
                userId,
                eventType,
                result,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                HttpContext.TraceIdentifier,
                ResolveCorrelationId(),
                details,
                HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist auth audit event {EventType}", eventType);
        }
    }

    private string? ResolveCorrelationId()
    {
        if (HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value) &&
            value is string correlationId &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        if (Request.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString().Trim();
        }

        return null;
    }

    private static Guid? TryGetUserIdFromAccessToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var value = token.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }
}
