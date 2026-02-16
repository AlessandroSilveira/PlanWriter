using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.Reports.Dtos.Queries;
using PlanWriter.Domain.Dtos.Reports;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(IMediator mediator, IUserService userService) : ControllerBase
{
    [HttpGet("writing")]
    [ProducesResponseType(typeof(WritingReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWritingReport(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? projectId = null,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<WritingReportPeriod>(period, true, out var parsedPeriod))
            return BadRequest("period must be one of: day, week, month.");

        var userId = userService.GetUserId(User);
        var response = await mediator.Send(
            new GetWritingReportQuery(userId, parsedPeriod, startDate, endDate, projectId),
            ct);

        return Ok(response);
    }
}
