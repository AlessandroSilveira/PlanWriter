using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.DTO;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(IProfileService profileService, IHttpContextAccessor ctx) : ControllerBase
{
    private Guid CurrentUserId()
    {
        var userIdValue = ctx.HttpContext?
            .User?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?
            .Value;

        if (string.IsNullOrWhiteSpace(userIdValue))
            throw new UnauthorizedAccessException("Usuário não autenticado");
        
        if (!Guid.TryParse(userIdValue, out var userId))
            throw new UnauthorizedAccessException("Claim de usuário inválida");

        return Guid.Parse(userIdValue);
    }


    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MyProfileDto>> GetMine()
        => Ok(await profileService.GetMineAsync(CurrentUserId()));

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<MyProfileDto>> UpdateMine([FromBody] UpdateMyProfileRequest req)
        => Ok(await profileService.UpdateMineAsync(CurrentUserId(), req));

    // Público
    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicProfileDto>> GetPublic(string slug)
        => Ok(await profileService.GetPublicAsync(slug));
}