using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PlanWriter.API.Security;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/admin/security/auth-audits")]
[AdminOnly]
public sealed class AdminAuthAuditsController(
    IAuthAuditReadRepository authAuditReadRepository,
    IOptions<AuthAuditOptions> options) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] string? result = null,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var settings = options.Value;
        var effectiveLimit = Math.Clamp(limit, 1, Math.Max(1, settings.MaxReadLimit));
        var effectiveFromUtc = fromUtc ?? DateTime.UtcNow.AddDays(-Math.Max(1, settings.RetentionDays));

        var logs = await authAuditReadRepository.GetAsync(
            effectiveFromUtc,
            toUtc,
            userId,
            eventType,
            result,
            effectiveLimit,
            ct);

        return Ok(logs);
    }
}
